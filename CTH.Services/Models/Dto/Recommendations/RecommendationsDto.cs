namespace CTH.Services.Models.Dto.Recommendations;

public sealed record RecommendationsDto
{
    public IReadOnlyCollection<TopicRecommendationDto> CriticalTopics { get; init; } = Array.Empty<TopicRecommendationDto>();
    public IReadOnlyCollection<TopicRecommendationDto> LeitnerTopics { get; init; } = Array.Empty<TopicRecommendationDto>();
    public IReadOnlyCollection<TopicRecommendationDto> UnstudiedTopics { get; init; } = Array.Empty<TopicRecommendationDto>();
    public int CriticalThreshold { get; init; } = 80;
}

