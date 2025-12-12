using CTH.Database.Entities.Public;

namespace CTH.Database.Repositories.Interfaces;

public interface ITaskRepository
{
    Task<IReadOnlyCollection<TaskItem>> GetTasksBySubjectAsync(long subjectId, string? searchQuery, CancellationToken cancellationToken);
    Task<long> CreateAsync(TaskItem task, CancellationToken cancellationToken);
}

