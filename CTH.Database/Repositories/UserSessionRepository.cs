using System;
using CTH.Database.Abstractions;
using CTH.Database.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace CTH.Database.Repositories;

public class UserSessionRepository : IUserSessionRepository
{
    private readonly ISqlExecutor _sqlExecutor;
    private readonly ILogger<UserSessionRepository> _logger;
    private readonly string _createSessionQuery;
    private readonly string _isSessionActiveQuery;
    private readonly string _revokeSessionQuery;

    public UserSessionRepository(
        ISqlExecutor sqlExecutor,
        ISqlQueryProvider sqlQueryProvider,
        ILogger<UserSessionRepository> logger)
    {
        _sqlExecutor = sqlExecutor;
        _logger = logger;
        _createSessionQuery = sqlQueryProvider.GetQuery("UserSessionUseCases/Commands/CreateSession");
        _isSessionActiveQuery = sqlQueryProvider.GetQuery("UserSessionUseCases/Queries/IsSessionActive");
        _revokeSessionQuery = sqlQueryProvider.GetQuery("UserSessionUseCases/Commands/RevokeSession");
    }

    public async Task CreateSessionAsync(long userId, Guid tokenId, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("user_id", NpgsqlDbType.Bigint) { Value = userId },
            new NpgsqlParameter("jti", NpgsqlDbType.Uuid) { Value = tokenId },
            new NpgsqlParameter("created_at", NpgsqlDbType.TimestampTz) { Value = DateTimeOffset.UtcNow },
            new NpgsqlParameter("expires_at", NpgsqlDbType.TimestampTz) { Value = expiresAt }
        };

        try
        {
            await _sqlExecutor.ExecuteAsync(_createSessionQuery, parameters, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to register session for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> IsSessionActiveAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("jti", NpgsqlDbType.Uuid) { Value = tokenId }
        };

        var result = await _sqlExecutor.QuerySingleAsync(
            _isSessionActiveQuery,
            reader => reader.GetBoolean(0),
            parameters,
            cancellationToken);

        return result;
    }

    public async Task<bool> RevokeSessionAsync(Guid tokenId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("jti", NpgsqlDbType.Uuid) { Value = tokenId },
            new NpgsqlParameter("revoked_at", NpgsqlDbType.TimestampTz) { Value = revokedAt }
        };

        var affected = await _sqlExecutor.ExecuteAsync(_revokeSessionQuery, parameters, cancellationToken);
        if (affected == 0)
        {
            _logger.LogWarning("Attempted to revoke missing or already revoked session {TokenId}", tokenId);
            return false;
        }

        return true;
    }
}
