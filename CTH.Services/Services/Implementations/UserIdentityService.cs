using System.Security.Claims;
using CTH.Services.Models.ResultModels;
using CTH.Services.Interfaces;
using PropTechPeople.Common.Constants;

namespace CTH.Services.Implementations
{
    public sealed class UserIdentityService : IUserIdentityService
    {
        public ClaimsIdentity CreateIdentityAsync(AuthorizeUserResult authorizeUserResult, CancellationToken cancellationToken)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentityCustomTypes.IdClaimType, authorizeUserResult!.Id.ToString()),
                new Claim(ClaimsIdentity.DefaultNameClaimType, authorizeUserResult.UserName),
                new Claim(ClaimTypes.Role, authorizeUserResult.RoleTypeId.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, "Bearer");

            return claimsIdentity;
        }
    }
}
