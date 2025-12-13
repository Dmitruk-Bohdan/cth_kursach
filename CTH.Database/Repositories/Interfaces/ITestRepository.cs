using CTH.Database.Entities.Public;
using CTH.Database.Models;

namespace CTH.Database.Repositories.Interfaces;

public interface ITestRepository
{
    Task<IReadOnlyCollection<Test>> GetPublishedTestsAsync(long userId, TestListFilter filter, CancellationToken cancellationToken);
    Task<Test?> GetTestByIdAsync(long testId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Test>> GetTestsByAuthorAndSubjectAsync(long authorId, long subjectId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Test>> GetMixedTestsByAuthorAndSubjectAsync(long authorId, long subjectId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TestTask>> GetTestTasksAsync(long testId, CancellationToken cancellationToken);
    Task<long> CreateAsync(Test test, CancellationToken cancellationToken);
    Task UpdateAsync(Test test, CancellationToken cancellationToken);
    Task DeleteAsync(long testId, CancellationToken cancellationToken);
    Task ReplaceTasksAsync(long testId, IReadOnlyCollection<TestTask> tasks, CancellationToken cancellationToken);
}
