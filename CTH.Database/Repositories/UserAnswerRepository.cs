using System.Text.Json;
using CTH.Database.Abstractions;
using CTH.Database.Entities.Public;
using CTH.Database.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace CTH.Database.Repositories;

public class UserAnswerRepository : IUserAnswerRepository
{
    private readonly ISqlExecutor _sqlExecutor;
    private readonly ILogger<UserAnswerRepository> _logger;
    private readonly string _upsertQuery;
    private readonly string _getByAttemptQuery;

    public UserAnswerRepository(
        ISqlExecutor sqlExecutor,
        ISqlQueryProvider sqlQueryProvider,
        ILogger<UserAnswerRepository> logger)
    {
        _sqlExecutor = sqlExecutor;
        _logger = logger;
        _upsertQuery = sqlQueryProvider.GetQuery("UserAnswerUseCases/Commands/UpsertAnswer");
        _getByAttemptQuery = sqlQueryProvider.GetQuery("UserAnswerUseCases/Queries/GetAnswersByAttempt");
    }

    public async Task UpsertAsync(long attemptId, long taskId, string givenAnswerJson, bool isCorrect, int? timeSpentSec, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("attempt_id", NpgsqlDbType.Bigint) { Value = attemptId },
            new NpgsqlParameter("task_id", NpgsqlDbType.Bigint) { Value = taskId },
            new NpgsqlParameter("given_answer", NpgsqlDbType.Jsonb) { Value = givenAnswerJson },
            new NpgsqlParameter("is_correct", NpgsqlDbType.Boolean) { Value = isCorrect },
            new NpgsqlParameter("time_spent_sec", NpgsqlDbType.Integer) { Value = (object?)timeSpentSec ?? DBNull.Value }
        };

        await _sqlExecutor.ExecuteAsync(_upsertQuery, parameters, cancellationToken);
    }

    public Task<IReadOnlyCollection<UserAnswer>> GetByAttemptIdAsync(long attemptId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("attempt_id", NpgsqlDbType.Bigint) { Value = attemptId }
        };

        return _sqlExecutor.QueryAsync(
            _getByAttemptQuery,
            reader => new UserAnswer
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                AttemptId = reader.GetInt64(reader.GetOrdinal("attempt_id")),
                TaskId = reader.GetInt64(reader.GetOrdinal("task_id")),
                GivenAnswer = JsonDocument.Parse(reader.GetString(reader.GetOrdinal("given_answer"))),
                IsCorrect = reader.GetBoolean(reader.GetOrdinal("is_correct")),
                TimeSpentSec = reader.IsDBNull(reader.GetOrdinal("time_spent_sec")) ? null : reader.GetInt32(reader.GetOrdinal("time_spent_sec")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at"))
            },
            parameters,
            cancellationToken);
    }
}
