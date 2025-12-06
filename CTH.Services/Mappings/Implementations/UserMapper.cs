using CTH.Database.Entities.Public;
using CTH.Services.Mappings.Interfaces;
using CTH.Services.Models.ResultModels;

namespace CTH.Services.Mappings.Implementations;

public class UserMapper :IUserMapper
{
    public AuthorizeUserResult UserAccountToAuthorizeResult(UserAccount account)
    {
        return new AuthorizeUserResult
        {
            Id = account.Id,
            UserName = account.UserName,
            RoleTypeId = account.RoleTypeId
        };
    }
}
