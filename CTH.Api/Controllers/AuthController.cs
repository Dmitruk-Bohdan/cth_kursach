using CTH.Api.Infrastructure;
using CTH.Common.Enums;
using CTH.Common.Helpers;
using CTH.Services.Extensions;
using CTH.Services.Models.Dto;
using CTH.Services.Models.ResponseModels;
using CTH.Services.Models.ResultModels;
using CTH.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System;
using System.Linq;
using System.Security.Claims;

namespace CTH.Api.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserIdentityService _userIdentityService;
        private readonly IUserAccoutManagmentService _userAccoutManagmentService;
        private readonly IUserSessionService _userSessionService;

        public AuthController(
            IUserIdentityService userIdentityService,
            IUserAccoutManagmentService userAccoutManagmentService,
            IUserSessionService userSessionService)
        {
            _userIdentityService = userIdentityService;
            _userAccoutManagmentService = userAccoutManagmentService;
            _userSessionService = userSessionService;

        }

        [AllowAnonymous]
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto, CancellationToken cancellationToken)
        {
            var roleType = EnumHelper.ParseFromInt<RoleTypeEnum>(registerDto.RoleTypeId);

            var registerUserResult = await _userAccoutManagmentService.RegisterUser(
                registerDto.Email,
                registerDto.UserName,
                registerDto.Password,
                roleType,
                cancellationToken);

            if (!registerUserResult.IsSuccessful)
            {
                return registerUserResult
                    .ConvertErrorResultToAnotherType<ClaimsIdentity>()
                    .ToActionResult();
            }

            var loginResponse = await BuildLoginResponseAsync(registerUserResult.Result!, cancellationToken);

            return Ok(loginResponse);
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("authorize")]
        public async Task<IActionResult> Authorize([FromBody] AuthorizeDto loginDto, CancellationToken cancellationToken)
        {
            var authorizeUserResult = await _userAccoutManagmentService.AuthorizeUser(loginDto.Email, loginDto.Password, cancellationToken);

            if (!authorizeUserResult.IsSuccessful)
            {
                return authorizeUserResult
                    .ConvertErrorResultToAnotherType<ClaimsIdentity>()
                    .ToActionResult();
            }

            var loginResponse = await BuildLoginResponseAsync(authorizeUserResult.Result!, cancellationToken);

            return Ok(loginResponse);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var tokenIdValue = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (!Guid.TryParse(tokenIdValue, out var tokenId))
            {
                return BadRequest("Token identifier is missing.");
            }

            var logoutResult = await _userSessionService.LogoutAsync(tokenId, cancellationToken);

            if (!logoutResult.IsSuccessful)
            {
                return logoutResult.ToActionResult();
            }

            return NoContent();
        }

        private async Task<LoginResponseModel> BuildLoginResponseAsync(AuthorizeUserResult authorizeUserResult, CancellationToken cancellationToken)
        {
            var userIdentity = _userIdentityService.CreateIdentityAsync(authorizeUserResult, cancellationToken);
            var claims = userIdentity!.Claims.ToList();

            var tokenId = Guid.NewGuid();
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, tokenId.ToString()));

            var now = DateTime.UtcNow;
            var expiresAt = now.Add(TimeSpan.FromDays(AuthOptions.Lifetime));
            var jwt = new JwtSecurityToken(
                issuer: AuthOptions.Issuer,
                audience: AuthOptions.Audience,
                notBefore: now,
                claims: claims,
                expires: expiresAt,
                signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            await _userSessionService.RegisterSessionAsync(authorizeUserResult.Id, tokenId, expiresAt, cancellationToken);

            return new LoginResponseModel
            {
                AccessToken = encodedJwt,
                UserName = userIdentity.Name!
            };
        }
    }
}
