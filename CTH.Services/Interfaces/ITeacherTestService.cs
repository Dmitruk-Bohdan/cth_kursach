using CTH.Services.Models.Dto.Tasks;
using CTH.Services.Models.Dto.Tests;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Interfaces;

public interface ITeacherTestService
{
    Task<HttpOperationResult<TestDetailsDto>> CreateTestAsync(long userId, bool isAdmin, CreateTestRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult> UpdateTestAsync(long userId, bool isAdmin, long testId, UpdateTestRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult> DeleteTestAsync(long userId, bool isAdmin, long testId, CancellationToken cancellationToken);
    Task<HttpOperationResult<TestDetailsDto>> GetTestAsync(long userId, bool isAdmin, long testId, CancellationToken cancellationToken);
    Task<HttpOperationResult<IReadOnlyCollection<TestListItemDto>>> GetMyTestsAsync(long userId, long subjectId, CancellationToken cancellationToken);
    Task<HttpOperationResult<IReadOnlyCollection<TaskListItemDto>>> GetTasksBySubjectAsync(long subjectId, string? searchQuery, CancellationToken cancellationToken);
    Task<HttpOperationResult<TaskListItemDto>> CreateTaskAsync(long userId, bool isAdmin, CreateTaskRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult<IReadOnlyCollection<TopicListItemDto>>> GetTopicsBySubjectAsync(long subjectId, CancellationToken cancellationToken);
}
