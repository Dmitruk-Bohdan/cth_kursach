using CTH.Services.Models.Dto.Recommendations;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Interfaces;

public interface IRecommendationsService
{
    Task<HttpOperationResult<RecommendationsDto>> GetRecommendationsAsync(
        long userId, 
        long subjectId, 
        int criticalThreshold = 80, 
        CancellationToken cancellationToken = default);
    
    Task<HttpOperationResult> UpdateCriticalThresholdAsync(
        long userId, 
        int newThreshold, 
        CancellationToken cancellationToken = default);
}

