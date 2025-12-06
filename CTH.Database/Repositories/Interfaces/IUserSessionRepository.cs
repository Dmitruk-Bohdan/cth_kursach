using System;

namespace CTH.Database.Repositories.Interfaces;

public interface IUserSessionRepository
{
    Task CreateSessionAsync(long userId, Guid tokenId, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    Task<bool> IsSessionActiveAsync(Guid tokenId, CancellationToken cancellationToken);
    Task<bool> RevokeSessionAsync(Guid tokenId, DateTimeOffset revokedAt, CancellationToken cancellationToken);
}
