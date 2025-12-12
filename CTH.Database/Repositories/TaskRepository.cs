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

    private readonly string _createTaskQuery;

    public TaskRepository(
        ISqlExecutor sqlExecutor,
        ISqlQueryProvider sqlQueryProvider,
        ILogger<TaskRepository> logger)
    {
        _sqlExecutor = sqlExecutor;
        _logger = logger;
        _getTasksBySubjectQuery = sqlQueryProvider.GetQuery("TaskUseCases/Queries/GetTasksBySubject");
        _createTaskQuery = sqlQueryProvider.GetQuery("TaskUseCases/Commands/CreateTask");
    }

    public async Task<IReadOnlyCollection<TaskItem>> GetTasksBySubjectAsync(long subjectId, string? searchQuery, CancellationToken cancellationToken)
    {
        // Определяем, является ли поисковый запрос числом
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

    public async Task<long> CreateAsync(TaskItem task, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = task.SubjectId },
            new NpgsqlParameter("topic_id", NpgsqlDbType.Bigint) { Value = (object?)task.TopicId ?? DBNull.Value },
            new NpgsqlParameter("task_type", NpgsqlDbType.Varchar) { Value = task.TaskType },
            new NpgsqlParameter("difficulty", NpgsqlDbType.Smallint) { Value = task.Difficulty },
            new NpgsqlParameter("statement", NpgsqlDbType.Text) { Value = task.Statement },
            new NpgsqlParameter("correct_answer", NpgsqlDbType.Text) { Value = System.Text.Json.JsonSerializer.Serialize(task.CorrectAnswer) }, // Обертываем строку в JSON
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
}

