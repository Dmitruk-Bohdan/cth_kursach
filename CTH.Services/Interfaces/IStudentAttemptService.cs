using CTH.Services.Models.Dto.Attempts;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Interfaces;

public interface IStudentAttemptService
{
    Task<HttpOperationResult<StartAttemptResponseDto>> StartAttemptAsync(long userId, long testId, CancellationToken cancellationToken);
    Task<HttpOperationResult> SubmitAnswerAsync(long userId, long attemptId, SubmitAnswerRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult> CompleteAttemptAsync(long userId, long attemptId, CompleteAttemptRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult> AbortAttemptAsync(long userId, long attemptId, CancellationToken cancellationToken);
    Task<HttpOperationResult> ResumeAttemptAsync(long userId, long attemptId, CancellationToken cancellationToken);
    Task<HttpOperationResult<AttemptDetailsDto>> GetAttemptAsync(long userId, long attemptId, CancellationToken cancellationToken);
    Task<HttpOperationResult<AttemptDetailsWithTasksDto>> GetAttemptDetailsWithTasksAsync(long userId, long attemptId, CancellationToken cancellationToken);
    Task<HttpOperationResult<IReadOnlyCollection<AttemptListItemDto>>> GetInProgressAttemptsAsync(long userId, CancellationToken cancellationToken);
    Task<HttpOperationResult<IReadOnlyCollection<AttemptListItemDto>>> GetAttemptsAsync(long userId, string? status, int limit, int offset, CancellationToken cancellationToken);
}
