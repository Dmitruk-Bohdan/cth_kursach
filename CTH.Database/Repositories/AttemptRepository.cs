using CTH.Database.Abstractions;
using CTH.Database.Entities.Public;
using CTH.Database.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace CTH.Database.Repositories;

public class AttemptRepository : IAttemptRepository
{
    private readonly ISqlExecutor _sqlExecutor;
    private readonly ILogger<AttemptRepository> _logger;
    private readonly string _createAttemptQuery;
    private readonly string _getAttemptByIdQuery;
    private readonly string _completeAttemptQuery;
    private readonly string _abortAttemptQuery;
    private readonly string _resumeAttemptQuery;
    private readonly string _getInProgressAttemptsQuery;
    private readonly string _getAttemptsByUserQuery;

    public AttemptRepository(
        ISqlExecutor sqlExecutor,
        ISqlQueryProvider sqlQueryProvider,
        ILogger<AttemptRepository> logger)
    {
        _sqlExecutor = sqlExecutor;
        _logger = logger;
        _createAttemptQuery = sqlQueryProvider.GetQuery("AttemptUseCases/Commands/CreateAttempt");
        _getAttemptByIdQuery = sqlQueryProvider.GetQuery("AttemptUseCases/Queries/GetAttemptById");
        _completeAttemptQuery = sqlQueryProvider.GetQuery("AttemptUseCases/Commands/CompleteAttempt");
        _abortAttemptQuery = sqlQueryProvider.GetQuery("AttemptUseCases/Commands/AbortAttempt");
        _resumeAttemptQuery = sqlQueryProvider.GetQuery("AttemptUseCases/Commands/ResumeAttempt");
        _getInProgressAttemptsQuery = sqlQueryProvider.GetQuery("AttemptUseCases/Queries/GetInProgressAttemptsByUser");
        _getAttemptsByUserQuery = sqlQueryProvider.GetQuery("AttemptUseCases/Queries/GetAttemptsByUser");
    }

    public async Task<Attempt> CreateAsync(long userId, long testId, long? assignmentId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("test_id", NpgsqlDbType.Bigint) { Value = testId },
            new NpgsqlParameter("user_id", NpgsqlDbType.Bigint) { Value = userId },
            new NpgsqlParameter("assignment_id", NpgsqlDbType.Bigint) { Value = (object?)assignmentId ?? DBNull.Value }
        };

        var attempt = await _sqlExecutor.QuerySingleAsync(
            _createAttemptQuery,
            reader => new Attempt
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                TestId = testId,
                UserId = userId,
                AssignmentId = assignmentId,
                StartedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("started_at")),
                Status = reader.GetString(reader.GetOrdinal("status"))
            },
            parameters,
            cancellationToken);

        if (attempt == null)
        {
            throw new InvalidOperationException("Failed to create attempt.");
        }

        return attempt;
    }

    public Task<Attempt?> GetByIdAsync(long attemptId, long userId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("attempt_id", NpgsqlDbType.Bigint) { Value = attemptId },
            new NpgsqlParameter("user_id", NpgsqlDbType.Bigint) { Value = userId }
        };

        return _sqlExecutor.QuerySingleAsync(
            _getAttemptByIdQuery,
            reader => new Attempt
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                TestId = reader.GetInt64(reader.GetOrdinal("test_id")),
                UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
                AssignmentId = reader.IsDBNull(reader.GetOrdinal("assignment_id")) ? null : reader.GetInt64(reader.GetOrdinal("assignment_id")),
                StartedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("started_at")),
                FinishedAt = reader.IsDBNull(reader.GetOrdinal("finished_at")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("finished_at")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                RawScore = reader.IsDBNull(reader.GetOrdinal("raw_score")) ? null : reader.GetDecimal(reader.GetOrdinal("raw_score")),
                ScaledScore = reader.IsDBNull(reader.GetOrdinal("scaled_score")) ? null : reader.GetDecimal(reader.GetOrdinal("scaled_score")),
                DurationSec = reader.IsDBNull(reader.GetOrdinal("duration_sec")) ? null : reader.GetInt32(reader.GetOrdinal("duration_sec")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at"))
            },
            parameters,
            cancellationToken);
    }

    public async Task<bool> CompleteAsync(long attemptId, long userId, decimal? rawScore, decimal? scaledScore, int? durationSec, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("attempt_id", NpgsqlDbType.Bigint) { Value = attemptId },
            new NpgsqlParameter("user_id", NpgsqlDbType.Bigint) { Value = userId },
            new NpgsqlParameter("raw_score", NpgsqlDbType.Numeric) { Value = (object?)rawScore ?? DBNull.Value },
            new NpgsqlParameter("scaled_score", NpgsqlDbType.Numeric) { Value = (object?)scaledScore ?? DBNull.Value },
            new NpgsqlParameter("duration_sec", NpgsqlDbType.Integer) { Value = (object?)durationSec ?? DBNull.Value }
        };

        var affected = await _sqlExecutor.ExecuteAsync(_completeAttemptQuery, parameters, cancellationToken);
        return affected > 0;
    }

    public async Task<bool> AbortAsync(long attemptId, long userId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("attempt_id", NpgsqlDbType.Bigint) { Value = attemptId },
            new NpgsqlParameter("user_id", NpgsqlDbType.Bigint) { Value = userId }
        };

        var affected = await _sqlExecutor.ExecuteAsync(_abortAttemptQuery, parameters, cancellationToken);
        return affected > 0;
    }

    public async Task<bool> ResumeAsync(long attemptId, long userId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("attempt_id", NpgsqlDbType.Bigint) { Value = attemptId },
            new NpgsqlParameter("user_id", NpgsqlDbType.Bigint) { Value = userId }
        };

        var affected = await _sqlExecutor.ExecuteAsync(_resumeAttemptQuery, parameters, cancellationToken);
        return affected > 0;
    }

    public Task<IReadOnlyCollection<Attempt>> GetInProgressAttemptsByUserAsync(long userId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("user_id", NpgsqlDbType.Bigint) { Value = userId }
        };

        return _sqlExecutor.QueryAsync(
            _getInProgressAttemptsQuery,
            reader => new Attempt
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                TestId = reader.GetInt64(reader.GetOrdinal("test_id")),
                UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
                AssignmentId = reader.IsDBNull(reader.GetOrdinal("assignment_id")) ? null : reader.GetInt64(reader.GetOrdinal("assignment_id")),
                StartedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("started_at")),
                FinishedAt = reader.IsDBNull(reader.GetOrdinal("finished_at")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("finished_at")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                RawScore = reader.IsDBNull(reader.GetOrdinal("raw_score")) ? null : reader.GetDecimal(reader.GetOrdinal("raw_score")),
                ScaledScore = reader.IsDBNull(reader.GetOrdinal("scaled_score")) ? null : reader.GetDecimal(reader.GetOrdinal("scaled_score")),
                DurationSec = reader.IsDBNull(reader.GetOrdinal("duration_sec")) ? null : reader.GetInt32(reader.GetOrdinal("duration_sec")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")),
                Test = new Test
                {
                    Id = reader.GetInt64(reader.GetOrdinal("test_id")),
                    Title = reader.GetString(reader.GetOrdinal("test_title")),
                    SubjectId = reader.GetInt64(reader.GetOrdinal("subject_id")),
                    Subject = new Subject
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("subject_id")),
                        SubjectName = reader.GetString(reader.GetOrdinal("subject_name"))
                    }
                }
            },
            parameters,
            cancellationToken);
    }

    public Task<IReadOnlyCollection<Attempt>> GetAttemptsByUserAsync(long userId, string? status, int limit, int offset, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("user_id", NpgsqlDbType.Bigint) { Value = userId },
            new NpgsqlParameter("status", NpgsqlDbType.Varchar) { Value = (object?)status ?? DBNull.Value },
            new NpgsqlParameter("limit", NpgsqlDbType.Integer) { Value = limit },
            new NpgsqlParameter("offset", NpgsqlDbType.Integer) { Value = offset }
        };

        return _sqlExecutor.QueryAsync(
            _getAttemptsByUserQuery,
            reader => new Attempt
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                TestId = reader.GetInt64(reader.GetOrdinal("test_id")),
                UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
                AssignmentId = reader.IsDBNull(reader.GetOrdinal("assignment_id")) ? null : reader.GetInt64(reader.GetOrdinal("assignment_id")),
                StartedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("started_at")),
                FinishedAt = reader.IsDBNull(reader.GetOrdinal("finished_at")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("finished_at")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                RawScore = reader.IsDBNull(reader.GetOrdinal("raw_score")) ? null : reader.GetDecimal(reader.GetOrdinal("raw_score")),
                ScaledScore = reader.IsDBNull(reader.GetOrdinal("scaled_score")) ? null : reader.GetDecimal(reader.GetOrdinal("scaled_score")),
                DurationSec = reader.IsDBNull(reader.GetOrdinal("duration_sec")) ? null : reader.GetInt32(reader.GetOrdinal("duration_sec")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")),
                Test = new Test
                {
                    Id = reader.GetInt64(reader.GetOrdinal("test_id")),
                    Title = reader.GetString(reader.GetOrdinal("test_title")),
                    SubjectId = reader.GetInt64(reader.GetOrdinal("subject_id")),
                    Subject = new Subject
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("subject_id")),
                        SubjectName = reader.GetString(reader.GetOrdinal("subject_name"))
                    }
                }
            },
            parameters,
            cancellationToken);
    }
}
