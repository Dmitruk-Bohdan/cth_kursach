using CTH.Database.Abstractions;
using CTH.Database.Entities.Public;
using CTH.Database.Models;
using CTH.Database.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace CTH.Database.Repositories;

public class TestRepository : ITestRepository
{
    private readonly ISqlExecutor _sqlExecutor;
    private readonly ILogger<TestRepository> _logger;
    private readonly string _getPublishedTestsQuery;
    private readonly string _getTestByIdQuery;
    private readonly string _getTestsByAuthorAndSubjectQuery;
    private readonly string _getTestTasksQuery;
    private readonly string _createTestQuery;
    private readonly string _updateTestQuery;
    private readonly string _deleteTestQuery;
    private readonly string _deleteTestTasksQuery;
    private readonly string _addTestTaskQuery;

    public TestRepository(
        ISqlExecutor sqlExecutor,
        ISqlQueryProvider sqlQueryProvider,
        ILogger<TestRepository> logger)
    {
        _sqlExecutor = sqlExecutor;
        _logger = logger;
        _getPublishedTestsQuery = sqlQueryProvider.GetQuery("TestUseCases/Queries/GetPublishedTests");
        _getTestByIdQuery = sqlQueryProvider.GetQuery("TestUseCases/Queries/GetTestById");
        _getTestsByAuthorAndSubjectQuery = sqlQueryProvider.GetQuery("TestUseCases/Queries/GetTestsByAuthorAndSubject");
        _getTestTasksQuery = sqlQueryProvider.GetQuery("TestUseCases/Queries/GetTestTasks");
        _createTestQuery = sqlQueryProvider.GetQuery("TestUseCases/Commands/CreateTest");
        _updateTestQuery = sqlQueryProvider.GetQuery("TestUseCases/Commands/UpdateTest");
        _deleteTestQuery = sqlQueryProvider.GetQuery("TestUseCases/Commands/DeleteTest");
        _deleteTestTasksQuery = sqlQueryProvider.GetQuery("TestUseCases/Commands/DeleteTestTasks");
        _addTestTaskQuery = sqlQueryProvider.GetQuery("TestUseCases/Commands/AddTestTask");
    }

    public async Task<IReadOnlyCollection<Test>> GetPublishedTestsAsync(long userId, TestListFilter filter, CancellationToken cancellationToken)
    {
        var titlePattern = string.IsNullOrWhiteSpace(filter.TitlePattern) 
            ? null 
            : $"%{filter.TitlePattern}%";

        var parameters = new[]
        {
            new NpgsqlParameter("user_id", NpgsqlDbType.Bigint) { Value = userId },
            new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = (object?)filter.SubjectId ?? DBNull.Value },
            new NpgsqlParameter("only_teachers", NpgsqlDbType.Boolean) { Value = filter.OnlyTeachers },
            new NpgsqlParameter("only_state_archive", NpgsqlDbType.Boolean) { Value = filter.OnlyStateArchive },
            new NpgsqlParameter("only_limited_attempts", NpgsqlDbType.Boolean) { Value = filter.OnlyLimitedAttempts },
            new NpgsqlParameter("title_pattern", NpgsqlDbType.Varchar) { Value = (object?)titlePattern ?? DBNull.Value },
            new NpgsqlParameter("mode", NpgsqlDbType.Varchar) { Value = (object?)filter.Mode ?? DBNull.Value }
        };

        var result = await _sqlExecutor.QueryAsync(
            _getPublishedTestsQuery,
            reader => new Test
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                SubjectId = reader.GetInt64(reader.GetOrdinal("subject_id")),
                TestKind = reader.GetString(reader.GetOrdinal("test_kind")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                AuthorId = reader.IsDBNull(reader.GetOrdinal("author_id")) ? null : reader.GetInt64(reader.GetOrdinal("author_id")),
                TimeLimitSec = reader.IsDBNull(reader.GetOrdinal("time_limit_sec")) ? null : reader.GetInt32(reader.GetOrdinal("time_limit_sec")),
                AttemptsAllowed = reader.IsDBNull(reader.GetOrdinal("attempts_allowed")) ? null : reader.GetInt16(reader.GetOrdinal("attempts_allowed")),
                Mode = reader.IsDBNull(reader.GetOrdinal("mode")) ? null : reader.GetString(reader.GetOrdinal("mode")),
                IsPublished = reader.GetBoolean(reader.GetOrdinal("is_published")),
                IsPublic = reader.GetBoolean(reader.GetOrdinal("is_public")),
                IsStateArchive = reader.GetBoolean(reader.GetOrdinal("is_state_archive")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")),
                Subject = new Subject
                {
                    Id = reader.GetInt64(reader.GetOrdinal("subject_id")),
                    SubjectName = reader.GetString(reader.GetOrdinal("subject_name"))
                }
            },
            parameters,
            cancellationToken);

        return result;
    }

    public async Task<Test?> GetTestByIdAsync(long testId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("test_id", NpgsqlDbType.Bigint) { Value = testId }
        };

        var test = await _sqlExecutor.QuerySingleAsync(
            _getTestByIdQuery,
            reader => new Test
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                SubjectId = reader.GetInt64(reader.GetOrdinal("subject_id")),
                TestKind = reader.GetString(reader.GetOrdinal("test_kind")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                AuthorId = reader.IsDBNull(reader.GetOrdinal("author_id")) ? null : reader.GetInt64(reader.GetOrdinal("author_id")),
                TimeLimitSec = reader.IsDBNull(reader.GetOrdinal("time_limit_sec")) ? null : reader.GetInt32(reader.GetOrdinal("time_limit_sec")),
                AttemptsAllowed = reader.IsDBNull(reader.GetOrdinal("attempts_allowed")) ? null : reader.GetInt16(reader.GetOrdinal("attempts_allowed")),
                Mode = reader.IsDBNull(reader.GetOrdinal("mode")) ? null : reader.GetString(reader.GetOrdinal("mode")),
                IsPublished = reader.GetBoolean(reader.GetOrdinal("is_published")),
                IsPublic = reader.GetBoolean(reader.GetOrdinal("is_public")),
                IsStateArchive = reader.GetBoolean(reader.GetOrdinal("is_state_archive")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")),
                Subject = new Subject
                {
                    Id = reader.GetInt64(reader.GetOrdinal("subject_id")),
                    SubjectName = reader.GetString(reader.GetOrdinal("subject_name"))
                }
            },
            parameters,
            cancellationToken);

        return test;
    }

    public async Task<IReadOnlyCollection<Test>> GetTestsByAuthorAndSubjectAsync(long authorId, long subjectId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("author_id", NpgsqlDbType.Bigint) { Value = authorId },
            new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = subjectId }
        };

        var result = await _sqlExecutor.QueryAsync(
            _getTestsByAuthorAndSubjectQuery,
            reader => new Test
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                SubjectId = reader.GetInt64(reader.GetOrdinal("subject_id")),
                TestKind = reader.GetString(reader.GetOrdinal("test_kind")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                AuthorId = reader.IsDBNull(reader.GetOrdinal("author_id")) ? null : reader.GetInt64(reader.GetOrdinal("author_id")),
                TimeLimitSec = reader.IsDBNull(reader.GetOrdinal("time_limit_sec")) ? null : reader.GetInt32(reader.GetOrdinal("time_limit_sec")),
                AttemptsAllowed = reader.IsDBNull(reader.GetOrdinal("attempts_allowed")) ? null : reader.GetInt16(reader.GetOrdinal("attempts_allowed")),
                Mode = reader.IsDBNull(reader.GetOrdinal("mode")) ? null : reader.GetString(reader.GetOrdinal("mode")),
                IsPublished = reader.GetBoolean(reader.GetOrdinal("is_published")),
                IsPublic = reader.GetBoolean(reader.GetOrdinal("is_public")),
                IsStateArchive = reader.GetBoolean(reader.GetOrdinal("is_state_archive")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")),
                Subject = new Subject
                {
                    Id = reader.GetInt64(reader.GetOrdinal("subject_id")),
                    SubjectName = reader.GetString(reader.GetOrdinal("subject_name"))
                }
            },
            parameters,
            cancellationToken);

        return result;
    }

    public async Task<IReadOnlyCollection<TestTask>> GetTestTasksAsync(long testId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("test_id", NpgsqlDbType.Bigint) { Value = testId }
        };

        var tasks = await _sqlExecutor.QueryAsync(
            _getTestTasksQuery,
            reader => new TestTask
            {
                Id = reader.GetInt64(reader.GetOrdinal("test_task_id")),
                TestId = reader.GetInt64(reader.GetOrdinal("test_id")),
                TaskId = reader.GetInt64(reader.GetOrdinal("task_id")),
                Position = reader.GetInt32(reader.GetOrdinal("position")),
                Weight = reader.IsDBNull(reader.GetOrdinal("weight")) ? null : reader.GetDecimal(reader.GetOrdinal("weight")),
                Task = new TaskItem
                {
                    Id = reader.GetInt64(reader.GetOrdinal("task_id")),
                    TaskType = reader.GetString(reader.GetOrdinal("task_type")),
                    Statement = reader.GetString(reader.GetOrdinal("statement")),
                    Explanation = reader.IsDBNull(reader.GetOrdinal("explanation")) ? null : reader.GetString(reader.GetOrdinal("explanation")),
                    Difficulty = reader.GetInt16(reader.GetOrdinal("difficulty")),
                    CorrectAnswer = reader.GetString(reader.GetOrdinal("correct_answer"))
                }
            },
            parameters,
            cancellationToken);

        return tasks;
    }

    public async Task<long> CreateAsync(Test test, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = test.SubjectId },
            new NpgsqlParameter("test_kind", NpgsqlDbType.Varchar) { Value = test.TestKind },
            new NpgsqlParameter("title", NpgsqlDbType.Varchar) { Value = test.Title },
            new NpgsqlParameter("author_id", NpgsqlDbType.Bigint) { Value = (object?)test.AuthorId ?? DBNull.Value },
            new NpgsqlParameter("time_limit_sec", NpgsqlDbType.Integer) { Value = (object?)test.TimeLimitSec ?? DBNull.Value },
            new NpgsqlParameter("attempts_allowed", NpgsqlDbType.Smallint) { Value = (object?)test.AttemptsAllowed ?? DBNull.Value },
            new NpgsqlParameter("mode", NpgsqlDbType.Varchar) { Value = (object?)test.Mode ?? DBNull.Value },
            new NpgsqlParameter("is_published", NpgsqlDbType.Boolean) { Value = test.IsPublished },
            new NpgsqlParameter("is_public", NpgsqlDbType.Boolean) { Value = test.IsPublic },
            new NpgsqlParameter("is_state_archive", NpgsqlDbType.Boolean) { Value = test.IsStateArchive }
        };

        var testId = await _sqlExecutor.QuerySingleAsync(
            _createTestQuery,
            reader => reader.GetInt64(0),
            parameters,
            cancellationToken);

        if (testId == 0)
        {
            throw new InvalidOperationException("Failed to create test");
        }

        return testId;
    }

    public Task UpdateAsync(Test test, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("test_id", NpgsqlDbType.Bigint) { Value = test.Id },
            new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = test.SubjectId },
            new NpgsqlParameter("test_kind", NpgsqlDbType.Varchar) { Value = test.TestKind },
            new NpgsqlParameter("title", NpgsqlDbType.Varchar) { Value = test.Title },
            new NpgsqlParameter("time_limit_sec", NpgsqlDbType.Integer) { Value = (object?)test.TimeLimitSec ?? DBNull.Value },
            new NpgsqlParameter("attempts_allowed", NpgsqlDbType.Smallint) { Value = (object?)test.AttemptsAllowed ?? DBNull.Value },
            new NpgsqlParameter("mode", NpgsqlDbType.Varchar) { Value = (object?)test.Mode ?? DBNull.Value },
            new NpgsqlParameter("is_published", NpgsqlDbType.Boolean) { Value = test.IsPublished },
            new NpgsqlParameter("is_public", NpgsqlDbType.Boolean) { Value = test.IsPublic },
            new NpgsqlParameter("is_state_archive", NpgsqlDbType.Boolean) { Value = test.IsStateArchive }
        };

        return _sqlExecutor.ExecuteAsync(_updateTestQuery, parameters, cancellationToken);
    }

    public Task DeleteAsync(long testId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("test_id", NpgsqlDbType.Bigint) { Value = testId }
        };

        return _sqlExecutor.ExecuteAsync(_deleteTestQuery, parameters, cancellationToken);
    }

    public async Task ReplaceTasksAsync(long testId, IReadOnlyCollection<TestTask> tasks, CancellationToken cancellationToken)
    {
        var deleteParams = new[]
        {
            new NpgsqlParameter("test_id", NpgsqlDbType.Bigint) { Value = testId }
        };

        await _sqlExecutor.ExecuteAsync(_deleteTestTasksQuery, deleteParams, cancellationToken);

        foreach (var task in tasks)
        {
            var insertParams = new[]
            {
                new NpgsqlParameter("test_id", NpgsqlDbType.Bigint) { Value = testId },
                new NpgsqlParameter("task_id", NpgsqlDbType.Bigint) { Value = task.TaskId },
                new NpgsqlParameter("position", NpgsqlDbType.Integer) { Value = task.Position },
                new NpgsqlParameter("weight", NpgsqlDbType.Numeric) { Value = (object?)task.Weight ?? DBNull.Value }
            };

            await _sqlExecutor.ExecuteAsync(_addTestTaskQuery, insertParams, cancellationToken);
        }
    }
}
