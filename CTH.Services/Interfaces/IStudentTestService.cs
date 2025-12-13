using CTH.Database.Models;
using CTH.Services.Models.Dto.Tests;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Interfaces;

public interface IStudentTestService
{
    Task<IReadOnlyCollection<TestListItemDto>> GetPublishedTestsAsync(long userId, TestListFilter filter, CancellationToken cancellationToken);
    Task<HttpOperationResult<TestDetailsDto>> GetTestDetailsAsync(long testId, long userId, CancellationToken cancellationToken);
    Task<HttpOperationResult<TestDetailsDto>> GenerateMixedTestAsync(long userId, GenerateMixedTestRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult<IReadOnlyCollection<TestListItemDto>>> GetMyMixedTestsAsync(long userId, long subjectId, CancellationToken cancellationToken);
    Task<HttpOperationResult> DeleteMixedTestAsync(long userId, long testId, CancellationToken cancellationToken);
}
