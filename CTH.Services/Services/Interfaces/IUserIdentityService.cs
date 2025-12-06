using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CTH.Services.Models.ResultModels;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Interfaces
{
    public interface IUserIdentityService
    {
        ClaimsIdentity CreateIdentityAsync(AuthorizeUserResult authorizeUserResult, CancellationToken cancellationToken);
    }
}
