using CTH.Database.Entities.Public;

namespace CTH.Database.Repositories.Interfaces;

public interface ITaskRepository
{
    Task<IReadOnlyCollection<TaskItem>> GetTasksBySubjectAsync(long subjectId, string? searchQuery, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TaskItem>> GetTasksByTopicsAndDifficultyAsync(IReadOnlyCollection<long> topicIds, IReadOnlyCollection<short> difficulties, int limitPerTopic, CancellationToken cancellationToken);
    Task<TaskItem?> GetTaskByIdAsync(long taskId, CancellationToken cancellationToken);
    Task<bool> IsTaskUsedInTeacherTestsAsync(long taskId, long teacherId, CancellationToken cancellationToken);
    Task<long> CreateAsync(TaskItem task, CancellationToken cancellationToken);
    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken);
}

