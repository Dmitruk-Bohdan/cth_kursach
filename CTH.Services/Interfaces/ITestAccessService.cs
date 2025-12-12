using CTH.Services.Models.Dto.TestAccess;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Interfaces;

public interface ITestAccessService
{
    Task<HttpOperationResult<IReadOnlyCollection<StudentAccessDto>>> GetStudentsByTestAsync(long testId, long teacherId, CancellationToken cancellationToken);
    Task<HttpOperationResult> AddStudentAccessAsync(long testId, long studentId, long teacherId, CancellationToken cancellationToken);
    Task<HttpOperationResult> RemoveStudentAccessAsync(long testId, long studentId, long teacherId, CancellationToken cancellationToken);
    Task<HttpOperationResult> SetStudentAccessListAsync(long testId, IReadOnlyCollection<long> studentIds, long teacherId, CancellationToken cancellationToken);
}

