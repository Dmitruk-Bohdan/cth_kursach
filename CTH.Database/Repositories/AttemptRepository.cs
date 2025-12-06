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
}
