using CTH.Services.Models.Dto.Teachers;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Interfaces;

public interface IStudentTeacherService
{
    Task<HttpOperationResult<TeacherDto>> JoinTeacherByCodeAsync(long studentId, string invitationCode, CancellationToken cancellationToken);
    Task<HttpOperationResult<IReadOnlyCollection<TeacherDto>>> GetMyTeachersAsync(long studentId, CancellationToken cancellationToken);
    Task<HttpOperationResult> RemoveTeacherAsync(long studentId, long teacherId, CancellationToken cancellationToken);
}

