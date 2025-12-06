using System.Net;
using CTH.Common.Enums;
using CTH.Common.Extensions;
using CTH.Common.Helpers;
using CTH.Database.Entities.Public;
using CTH.Database.Repositories.Interfaces;
using CTH.Services.Interfaces;
using CTH.Services.Mappings.Interfaces;
using CTH.Services.Models.ResultModels;
using Microsoft.Extensions.Logging;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Implementations;

public class UserAccoutManagmentService : IUserAccoutManagmentService
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IUserMapper _userMapper;
    private readonly ILogger<UserAccoutManagmentService> _logger;

    public UserAccoutManagmentService(
        IUserAccountRepository userAccountRepository,
        IUserMapper userMapper,
        ILogger<UserAccoutManagmentService> logger)
    {
        _userAccountRepository = userAccountRepository;
        _userMapper = userMapper;
        _logger = logger;
    }

    public async Task<HttpOperationResult<AuthorizeUserResult>> RegisterUser(
        string email,
        string username,
        string password,
        RoleTypeEnum roleType,
        CancellationToken cancellationToken)
    {
        var fetchedUser = await _userAccountRepository.GetByEmailAsync(email, cancellationToken);
        if (fetchedUser != null)
        {
            var errorMessage = $"User with email {email} already exist";

            return new HttpOperationResult<AuthorizeUserResult>
            {
                Status = HttpStatusCode.Conflict,
                Error = errorMessage
            };
        }

        var passwordHash = PasswordHelper.HashPassword(password);
        var now = DateTimeOffset.UtcNow;

        var newUser = new UserAccount
        {
            Email = email,
            UserName = username,
            PasswordHash = passwordHash,
            RoleTypeId = roleType.ToInt(),
            LastLoginAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        var createdUserId = await _userAccountRepository.InsertAsync(newUser, cancellationToken);
        newUser.Id = createdUserId;

        return new HttpOperationResult<AuthorizeUserResult>
        {
            Result = _userMapper.UserAccountToAuthorizeResult(newUser),
            Status = HttpStatusCode.OK
        };
    }

    public async Task<HttpOperationResult<AuthorizeUserResult>> AuthorizeUser(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var passwordHash = PasswordHelper.HashPassword(password);

        var fetchedUser = await _userAccountRepository.GetByEmailAsync(email, cancellationToken);

        if (fetchedUser == null || !string.Equals(passwordHash, fetchedUser.PasswordHash, StringComparison.Ordinal))
        {
            var errorMessage = $"Invalid credentials provided for {email}";
            _logger.LogWarning(errorMessage);

            return new HttpOperationResult<AuthorizeUserResult>
            {
                Status = HttpStatusCode.Unauthorized,
                Error = "Invalid email or password."
            };
        }

        var now = DateTimeOffset.UtcNow;
        await _userAccountRepository.UpdateLastLoginAsync(fetchedUser.Id, now, cancellationToken);
        fetchedUser.LastLoginAt = now;

        return new HttpOperationResult<AuthorizeUserResult>
        {
            Result = _userMapper.UserAccountToAuthorizeResult(fetchedUser),
            Status = HttpStatusCode.OK
        };
    }
}
