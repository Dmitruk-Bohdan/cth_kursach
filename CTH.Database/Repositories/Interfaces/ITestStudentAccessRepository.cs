using CTH.Database.Entities.Public;

namespace CTH.Database.Repositories.Interfaces;

public interface ITestStudentAccessRepository
{
    Task<long> AddStudentAccessAsync(long testId, long studentId, CancellationToken cancellationToken);
    Task RemoveStudentAccessAsync(long testId, long studentId, CancellationToken cancellationToken);
    Task RemoveAllStudentAccessAsync(long testId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TestStudentAccess>> GetStudentsByTestIdAsync(long testId, CancellationToken cancellationToken);
}

