using CTH.Services.Models.Dto.Invitations;
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
}

