using CTH.Database.Entities.Public;

namespace CTH.Database.Repositories.Interfaces;

public interface IUserStatsRepository
{
    Task<IReadOnlyCollection<UserStats>> GetStatisticsBySubjectAsync(long userId, long? subjectId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<UserStats>> GetStatisticsByTopicAsync(long userId, long? subjectId, CancellationToken cancellationToken);
}

