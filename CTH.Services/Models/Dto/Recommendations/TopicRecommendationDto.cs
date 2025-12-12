namespace CTH.Services.Models.Dto.Recommendations;

public sealed record TopicRecommendationDto
{
    public long TopicId { get; init; }
    public string TopicName { get; init; } = string.Empty;
    public string? TopicCode { get; init; }
    public int? AttemptsTotal { get; init; }
    public int? CorrectTotal { get; init; }
    public decimal? AccuracyPercentage { get; init; }
    public DateTimeOffset? LastAttemptAt { get; init; }
    public int? SuccessfulRepetitions { get; init; }
    public int? RepetitionIntervalDays { get; init; }
}

