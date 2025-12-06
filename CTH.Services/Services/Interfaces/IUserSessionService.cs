using System;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Interfaces;

public interface IUserSessionService
{
    Task RegisterSessionAsync(long userId, Guid tokenId, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    Task<HttpOperationResult> LogoutAsync(Guid tokenId, CancellationToken cancellationToken);
}
