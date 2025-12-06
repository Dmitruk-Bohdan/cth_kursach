using CTH.Common.Enums;
using CTH.Services.Models.ResultModels;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Interfaces
{
    public interface IUserAccoutManagmentService
    {
        Task<HttpOperationResult<AuthorizeUserResult>> AuthorizeUser(
            string email,
            string password,
            CancellationToken cancellationToken);
        Task<HttpOperationResult<AuthorizeUserResult>> RegisterUser(
            string email,
            string userName,
            string password,
            RoleTypeEnum roleType,
            CancellationToken cancellationToken);
    }
}
