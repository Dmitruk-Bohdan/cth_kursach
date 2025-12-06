using CTH.Database.Entities.Public;

namespace CTH.Database.Repositories.Interfaces;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<long> InsertAsync(UserAccount user, CancellationToken cancellationToken);
    Task UpdateLastLoginAsync(long userId, DateTimeOffset lastLoginAt, CancellationToken cancellationToken);
}
