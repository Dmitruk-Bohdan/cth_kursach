using System;
using System.Net;
using CTH.Database.Repositories.Interfaces;
using CTH.Services.Interfaces;
using Microsoft.Extensions.Logging;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Implementations;

public class UserSessionService : IUserSessionService
{
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly ILogger<UserSessionService> _logger;

    public UserSessionService(
        IUserSessionRepository userSessionRepository,
        ILogger<UserSessionService> logger)
    {
        _userSessionRepository = userSessionRepository;
        _logger = logger;
    }

    public async Task RegisterSessionAsync(long userId, Guid tokenId, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        await _userSessionRepository.CreateSessionAsync(userId, tokenId, expiresAt, cancellationToken);
    }

    public async Task<HttpOperationResult> LogoutAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        var isRevoked = await _userSessionRepository.RevokeSessionAsync(tokenId, DateTimeOffset.UtcNow, cancellationToken);

        if (!isRevoked)
        {
            var message = $"Failed to revoke token {tokenId}";
            _logger.LogWarning(message);
            return new HttpOperationResult
            {
                Status = HttpStatusCode.NotFound,
                Error = message
            };
        }

        return new HttpOperationResult(HttpStatusCode.NoContent);
    }
}
