using CTH.Database.Abstractions;
using CTH.Database.Entities.Public;
using CTH.Database.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;

namespace CTH.Database.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly ISqlExecutor _sqlExecutor;
    private readonly ILogger<TaskRepository> _logger;
    private readonly string _getTasksBySubjectQuery;
    private readonly string _getTasksByTopicsAndDifficultyQuery;
    private readonly string _getTaskByIdQuery;
    private readonly string _checkTaskInTeacherTestsQuery;
    private readonly string _createTaskQuery;
    private readonly string _updateTaskQuery;

    public TaskRepository(
        ISqlExecutor sqlExecutor,
        ISqlQueryProvider sqlQueryProvider,
        ILogger<TaskRepository> logger)
    {
        _sqlExecutor = sqlExecutor;
        _logger = logger;
        _getTasksBySubjectQuery = sqlQueryProvider.GetQuery("TaskUseCases/Queries/GetTasksBySubject");
        _getTasksByTopicsAndDifficultyQuery = sqlQueryProvider.GetQuery("TaskUseCases/Queries/GetTasksByTopicsAndDifficulty");
        _getTaskByIdQuery = sqlQueryProvider.GetQuery("TaskUseCases/Queries/GetTaskById");
        _checkTaskInTeacherTestsQuery = sqlQueryProvider.GetQuery("TaskUseCases/Queries/CheckTaskInTeacherTests");
        _createTaskQuery = sqlQueryProvider.GetQuery("TaskUseCases/Commands/CreateTask");
        _updateTaskQuery = sqlQueryProvider.GetQuery("TaskUseCases/Commands/UpdateTask");
    }

    public async Task<IReadOnlyCollection<TaskItem>> GetTasksBySubjectAsync(long subjectId, string? searchQuery, CancellationToken cancellationToken)
    {
        var searchIsNumber = false;
        if (!string.IsNullOrWhiteSpace(searchQuery) && long.TryParse(searchQuery, out _))
        {
            searchIsNumber = true;
        }

        var parameters = new[]
        {
            new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = subjectId },
            new NpgsqlParameter("search_query", NpgsqlDbType.Varchar) { Value = (object?)searchQuery ?? DBNull.Value },
            new NpgsqlParameter("search_is_number", NpgsqlDbType.Boolean) { Value = searchIsNumber }
        };

        var result = await _sqlExecutor.QueryAsync(
            _getTasksBySubjectQuery,
            reader => new TaskItem
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                SubjectId = reader.GetInt64(reader.GetOrdinal("subject_id")),
                TopicId = reader.IsDBNull(reader.GetOrdinal("topic_id")) ? null : reader.GetInt64(reader.GetOrdinal("topic_id")),
                TaskType = reader.GetString(reader.GetOrdinal("task_type")),
                Difficulty = reader.GetInt16(reader.GetOrdinal("difficulty")),
                Statement = reader.GetString(reader.GetOrdinal("statement")),
                CorrectAnswer = reader.GetString(reader.GetOrdinal("correct_answer")),
                Explanation = reader.IsDBNull(reader.GetOrdinal("explanation")) ? null : reader.GetString(reader.GetOrdinal("explanation")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                Topic = reader.IsDBNull(reader.GetOrdinal("topic_id")) 
                    ? null 
                    : new Topic
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("topic_id")),
                        SubjectId = reader.GetInt64(reader.GetOrdinal("subject_id")),
                        TopicName = reader.IsDBNull(reader.GetOrdinal("topic_name")) 
                            ? string.Empty 
                            : reader.GetString(reader.GetOrdinal("topic_name")),
                        TopicCode = reader.IsDBNull(reader.GetOrdinal("topic_code")) 
                            ? null 
                            : reader.GetString(reader.GetOrdinal("topic_code"))
                    }
            },
            parameters,
            cancellationToken);

        return result;
    }

    public async Task<IReadOnlyCollection<TaskItem>> GetTasksByTopicsAndDifficultyAsync(IReadOnlyCollection<long> topicIds, IReadOnlyCollection<short> difficulties, int limitPerTopic, CancellationToken cancellationToken)
    {
        if (topicIds == null || topicIds.Count == 0)
        {
            return Array.Empty<TaskItem>();
        }

        if (difficulties == null || difficulties.Count == 0)
        {
            return Array.Empty<TaskItem>();
        }

        var topicIdsArray = topicIds.ToArray();
        var difficultiesArray = difficulties.ToArray();

        var parameters = new[]
        {
            new NpgsqlParameter("topic_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = topicIdsArray },
            new NpgsqlParameter("difficulties", NpgsqlDbType.Array | NpgsqlDbType.Smallint) { Value = difficultiesArray },
            new NpgsqlParameter("limit_per_topic", NpgsqlDbType.Integer) { Value = limitPerTopic }
        };

        var result = await _sqlExecutor.QueryAsync(
            _getTasksByTopicsAndDifficultyQuery,
            reader => new TaskItem
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                SubjectId = reader.GetInt64(reader.GetOrdinal("subject_id")),
                TopicId = reader.IsDBNull(reader.GetOrdinal("topic_id")) ? null : reader.GetInt64(reader.GetOrdinal("topic_id")),
                TaskType = reader.GetString(reader.GetOrdinal("task_type")),
                Difficulty = reader.GetInt16(reader.GetOrdinal("difficulty")),
                Statement = reader.GetString(reader.GetOrdinal("statement")),
                Explanation = reader.IsDBNull(reader.GetOrdinal("explanation")) ? null : reader.GetString(reader.GetOrdinal("explanation")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                Topic = reader.IsDBNull(reader.GetOrdinal("topic_id")) 
                    ? null 
                    : new Topic
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("topic_id")),
                        SubjectId = reader.GetInt64(reader.GetOrdinal("subject_id")),
                        TopicName = reader.IsDBNull(reader.GetOrdinal("topic_name")) 
                            ? string.Empty 
                            : reader.GetString(reader.GetOrdinal("topic_name")),
                        TopicCode = reader.IsDBNull(reader.GetOrdinal("topic_code")) 
                            ? null 
                            : reader.GetString(reader.GetOrdinal("topic_code"))
                    }
            },
            parameters,
            cancellationToken);

        return result;
    }

    public async Task<bool> IsTaskUsedInTeacherTestsAsync(long taskId, long teacherId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("task_id", NpgsqlDbType.Bigint) { Value = taskId },
            new NpgsqlParameter("teacher_id", NpgsqlDbType.Bigint) { Value = teacherId }
        };

        var result = await _sqlExecutor.QuerySingleAsync(
            _checkTaskInTeacherTestsQuery,
            reader => reader.GetBoolean(reader.GetOrdinal("is_used")),
            parameters,
            cancellationToken);

        return result;
    }

    public async Task<long> CreateAsync(TaskItem task, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = task.SubjectId },
            new NpgsqlParameter("topic_id", NpgsqlDbType.Bigint) { Value = (object?)task.TopicId ?? DBNull.Value },
            new NpgsqlParameter("task_type", NpgsqlDbType.Varchar) { Value = task.TaskType },
            new NpgsqlParameter("difficulty", NpgsqlDbType.Smallint) { Value = task.Difficulty },
            new NpgsqlParameter("statement", NpgsqlDbType.Text) { Value = task.Statement },
            new NpgsqlParameter("correct_answer", NpgsqlDbType.Text) { Value = System.Text.Json.JsonSerializer.Serialize(task.CorrectAnswer) }, 
            new NpgsqlParameter("explanation", NpgsqlDbType.Text) { Value = (object?)task.Explanation ?? DBNull.Value },
            new NpgsqlParameter("is_active", NpgsqlDbType.Boolean) { Value = task.IsActive }
        };

        var taskId = await _sqlExecutor.QuerySingleAsync(
            _createTaskQuery,
            reader => reader.GetInt64(0),
            parameters,
            cancellationToken);

        if (taskId == 0)
        {
            throw new InvalidOperationException("Failed to create task");
        }

        return taskId;
    }

    public async Task<TaskItem?> GetTaskByIdAsync(long taskId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("task_id", NpgsqlDbType.Bigint) { Value = taskId }
        };

        var result = await _sqlExecutor.QuerySingleAsync(
            _getTaskByIdQuery,
            reader => new TaskItem
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                SubjectId = reader.GetInt64(reader.GetOrdinal("subject_id")),
                TopicId = reader.IsDBNull(reader.GetOrdinal("topic_id")) ? null : reader.GetInt64(reader.GetOrdinal("topic_id")),
                TaskType = reader.GetString(reader.GetOrdinal("task_type")),
                Difficulty = reader.GetInt16(reader.GetOrdinal("difficulty")),
                Statement = reader.GetString(reader.GetOrdinal("statement")),
                CorrectAnswer = reader.GetString(reader.GetOrdinal("correct_answer")),
                Explanation = reader.IsDBNull(reader.GetOrdinal("explanation")) ? null : reader.GetString(reader.GetOrdinal("explanation")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                Topic = reader.IsDBNull(reader.GetOrdinal("topic_id")) 
                    ? null 
                    : new Topic
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("topic_id")),
                        SubjectId = reader.GetInt64(reader.GetOrdinal("subject_id")),
                        TopicName = reader.IsDBNull(reader.GetOrdinal("topic_name")) 
                            ? string.Empty 
                            : reader.GetString(reader.GetOrdinal("topic_name")),
                        TopicCode = reader.IsDBNull(reader.GetOrdinal("topic_code")) 
                            ? null 
                            : reader.GetString(reader.GetOrdinal("topic_code"))
                    }
            },
            parameters,
            cancellationToken);

        return result;
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("task_id", NpgsqlDbType.Bigint) { Value = task.Id },
            new NpgsqlParameter("topic_id", NpgsqlDbType.Bigint) { Value = (object?)task.TopicId ?? DBNull.Value },
            new NpgsqlParameter("task_type", NpgsqlDbType.Varchar) { Value = string.IsNullOrEmpty(task.TaskType) ? DBNull.Value : task.TaskType },
            new NpgsqlParameter("difficulty", NpgsqlDbType.Smallint) { Value = task.Difficulty == 0 ? DBNull.Value : task.Difficulty },
            new NpgsqlParameter("statement", NpgsqlDbType.Text) { Value = string.IsNullOrEmpty(task.Statement) ? DBNull.Value : task.Statement },
            new NpgsqlParameter("correct_answer", NpgsqlDbType.Text) { Value = string.IsNullOrEmpty(task.CorrectAnswer) ? DBNull.Value : task.CorrectAnswer },
            new NpgsqlParameter("explanation", NpgsqlDbType.Text) { Value = (object?)task.Explanation ?? DBNull.Value },
            new NpgsqlParameter("is_active", NpgsqlDbType.Boolean) { Value = task.IsActive }
        };

        await _sqlExecutor.ExecuteAsync(_updateTaskQuery, parameters, cancellationToken);
    }
}

