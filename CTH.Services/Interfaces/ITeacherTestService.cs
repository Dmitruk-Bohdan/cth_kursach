using CTH.Services.Models.Dto.Tests;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Interfaces;

public interface ITeacherTestService
{
    Task<HttpOperationResult<TestDetailsDto>> CreateTestAsync(long userId, bool isAdmin, CreateTestRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult> UpdateTestAsync(long userId, bool isAdmin, long testId, UpdateTestRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult> DeleteTestAsync(long userId, bool isAdmin, long testId, CancellationToken cancellationToken);
    Task<HttpOperationResult<TestDetailsDto>> GetTestAsync(long userId, bool isAdmin, long testId, CancellationToken cancellationToken);
}
