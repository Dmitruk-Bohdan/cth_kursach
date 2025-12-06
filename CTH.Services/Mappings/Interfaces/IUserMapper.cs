using CTH.Database.Entities.Public;
using CTH.Services.Models.ResultModels;

namespace CTH.Services.Mappings.Interfaces;

public interface IUserMapper
{
    AuthorizeUserResult UserAccountToAuthorizeResult(UserAccount model);
}
