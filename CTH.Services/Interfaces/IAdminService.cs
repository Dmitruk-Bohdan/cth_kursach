using Admin = CTH.Services.Models.Dto.Admin;
using Tasks = CTH.Services.Models.Dto.Tasks;
using CTH.Services.Models.Dto.Tests;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Interfaces;

public interface IAdminService
{
    Task<HttpOperationResult<IReadOnlyCollection<Admin.UserListItemDto>>> GetAllUsersAsync(CancellationToken cancellationToken);
    Task<HttpOperationResult<Admin.UserDetailsDto>> CreateUserAsync(Admin.CreateUserRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult<Admin.UserDetailsDto>> UpdateUserAsync(long userId, Admin.UpdateUserRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult> BlockUserAsync(long userId, CancellationToken cancellationToken);
    Task<HttpOperationResult> UnblockUserAsync(long userId, CancellationToken cancellationToken);
    Task<HttpOperationResult> DeleteUserAsync(long userId, CancellationToken cancellationToken);

    Task<HttpOperationResult<IReadOnlyCollection<Admin.SubjectListItemDto>>> GetAllSubjectsAsync(CancellationToken cancellationToken);
    Task<HttpOperationResult<Admin.SubjectDetailsDto>> CreateSubjectAsync(Admin.CreateSubjectRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult<Admin.SubjectDetailsDto>> UpdateSubjectAsync(long subjectId, Admin.UpdateSubjectRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult> DeleteSubjectAsync(long subjectId, CancellationToken cancellationToken);

    Task<HttpOperationResult<IReadOnlyCollection<Admin.TopicListItemDto>>> GetAllTopicsAsync(long? subjectId, CancellationToken cancellationToken);
    Task<HttpOperationResult<Admin.TopicDetailsDto>> CreateTopicAsync(Admin.CreateTopicRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult<Admin.TopicDetailsDto>> UpdateTopicAsync(long topicId, Admin.UpdateTopicRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult> DeleteTopicAsync(long topicId, CancellationToken cancellationToken);

    Task<HttpOperationResult<IReadOnlyCollection<Tasks.TaskListItemDto>>> GetAllTasksAsync(Admin.TaskFilterDto? filter, CancellationToken cancellationToken);
    Task<HttpOperationResult<Admin.TaskDetailsDto>> CreateTaskAsync(Tasks.CreateTaskRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult<Admin.TaskDetailsDto>> UpdateTaskAsync(long taskId, Tasks.UpdateTaskRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult> ActivateTaskAsync(long taskId, CancellationToken cancellationToken);
    Task<HttpOperationResult> DeactivateTaskAsync(long taskId, CancellationToken cancellationToken);
    Task<HttpOperationResult> DeleteTaskAsync(long taskId, CancellationToken cancellationToken);

    Task<HttpOperationResult<IReadOnlyCollection<Admin.TestListItemDto>>> GetAllTestsAsync(Admin.TestFilterDto? filter, CancellationToken cancellationToken);
    Task<HttpOperationResult<TestDetailsDto>> CreateTestAsync(CreateTestRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult<TestDetailsDto>> UpdateTestAsync(long testId, UpdateTestRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult> DeleteTestAsync(long testId, CancellationToken cancellationToken);

    Task<HttpOperationResult<IReadOnlyCollection<Admin.InvitationCodeListItemDto>>> GetAllInvitationCodesAsync(long? teacherId, string? status, CancellationToken cancellationToken);
    Task<HttpOperationResult<Admin.InvitationCodeDetailsDto>> CreateInvitationCodeAsync(Admin.CreateInvitationCodeRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult<Admin.InvitationCodeDetailsDto>> UpdateInvitationCodeAsync(long invitationCodeId, Admin.UpdateInvitationCodeRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult> DeleteInvitationCodeAsync(long invitationCodeId, CancellationToken cancellationToken);
}

