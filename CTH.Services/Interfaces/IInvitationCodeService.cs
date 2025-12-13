using CTH.Services.Models.Dto.Attempts;
using CTH.Services.Models.Dto.Invitations;
using CTH.Services.Models.Dto.Statistics;
using CTH.Services.Models.Dto.Students;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Interfaces;

public interface IInvitationCodeService
{
    Task<HttpOperationResult<InvitationCodeDto>> CreateInvitationCodeAsync(long teacherId, CreateInvitationCodeRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult<IReadOnlyCollection<InvitationCodeDto>>> GetInvitationCodesByTeacherAsync(long teacherId, CancellationToken cancellationToken);
    Task<HttpOperationResult> RevokeInvitationCodeAsync(long teacherId, long invitationCodeId, CancellationToken cancellationToken);
    Task<HttpOperationResult> DeleteInvitationCodeAsync(long teacherId, long invitationCodeId, CancellationToken cancellationToken);
    Task<HttpOperationResult<IReadOnlyCollection<StudentDto>>> GetMyStudentsAsync(long teacherId, CancellationToken cancellationToken);
    Task<HttpOperationResult> RemoveStudentAsync(long teacherId, long studentId, CancellationToken cancellationToken);
    Task<HttpOperationResult<IReadOnlyCollection<AttemptListItemDto>>> GetStudentAttemptsAsync(long teacherId, long studentId, string? status, int limit, int offset, CancellationToken cancellationToken);
    Task<HttpOperationResult<AttemptDetailsWithTasksDto>> GetStudentAttemptDetailsWithTasksAsync(long teacherId, long studentId, long attemptId, CancellationToken cancellationToken);
    Task<HttpOperationResult<IReadOnlyCollection<SubjectDto>>> GetStudentStatisticsSubjectsAsync(long teacherId, long studentId, CancellationToken cancellationToken);
    Task<HttpOperationResult<SubjectStatisticsDto>> GetStudentSubjectStatisticsAsync(long teacherId, long studentId, long subjectId, CancellationToken cancellationToken);
}

