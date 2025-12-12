using CTH.Database.Entities.Public;

namespace CTH.Database.Repositories.Interfaces;

public interface IInvitationCodeRepository
{
    Task<long> CreateAsync(InvitationCode invitationCode, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<InvitationCode>> GetByTeacherIdAsync(long teacherId, CancellationToken cancellationToken);
    Task<InvitationCode?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task UpdateAsync(InvitationCode invitationCode, CancellationToken cancellationToken);
    Task DeleteAsync(long id, CancellationToken cancellationToken);
}

