using CTH.Services.Models.Dto.Attempts;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Interfaces;

public interface IStudentAttemptService
{
    Task<HttpOperationResult<StartAttemptResponseDto>> StartAttemptAsync(long userId, long testId, CancellationToken cancellationToken);
    Task<HttpOperationResult> SubmitAnswerAsync(long userId, long attemptId, SubmitAnswerRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult> CompleteAttemptAsync(long userId, long attemptId, CompleteAttemptRequestDto request, CancellationToken cancellationToken);
    Task<HttpOperationResult<AttemptDetailsDto>> GetAttemptAsync(long userId, long attemptId, CancellationToken cancellationToken);
}
