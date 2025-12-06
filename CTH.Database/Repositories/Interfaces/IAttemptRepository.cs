using CTH.Database.Entities.Public;

namespace CTH.Database.Repositories.Interfaces;

public interface IAttemptRepository
{
    Task<Attempt> CreateAsync(long userId, long testId, long? assignmentId, CancellationToken cancellationToken);
    Task<Attempt?> GetByIdAsync(long attemptId, long userId, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(long attemptId, long userId, decimal? rawScore, decimal? scaledScore, int? durationSec, CancellationToken cancellationToken);
}
