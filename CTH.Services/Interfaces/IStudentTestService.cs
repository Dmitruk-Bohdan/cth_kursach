using CTH.Services.Models.Dto.Tests;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Interfaces;

public interface IStudentTestService
{
    Task<IReadOnlyCollection<TestListItemDto>> GetPublishedTestsAsync(long? subjectId, CancellationToken cancellationToken);
    Task<HttpOperationResult<TestDetailsDto>> GetTestDetailsAsync(long testId, CancellationToken cancellationToken);
}
