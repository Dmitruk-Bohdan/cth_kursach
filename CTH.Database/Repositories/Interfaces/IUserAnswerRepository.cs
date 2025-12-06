using CTH.Database.Entities.Public;

namespace CTH.Database.Repositories.Interfaces;

public interface IUserAnswerRepository
{
    Task UpsertAsync(long attemptId, long taskId, string givenAnswerJson, bool isCorrect, int? timeSpentSec, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<UserAnswer>> GetByAttemptIdAsync(long attemptId, CancellationToken cancellationToken);
}
