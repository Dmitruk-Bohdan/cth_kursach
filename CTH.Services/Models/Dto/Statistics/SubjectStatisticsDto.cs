namespace CTH.Services.Models.Dto.Statistics;

public class SubjectStatisticsDto
{
    public decimal? OverallAccuracyPercentage { get; set; }
    public int OverallAttemptsTotal { get; set; }
    public int OverallCorrectTotal { get; set; }
    public IReadOnlyCollection<TopicStatisticsDto> Top3ErrorTopics { get; set; } = Array.Empty<TopicStatisticsDto>();
    public IReadOnlyCollection<TopicStatisticsDto> OtherTopics { get; set; } = Array.Empty<TopicStatisticsDto>();
    public IReadOnlyCollection<TopicStatisticsDto> UnattemptedTopics { get; set; } = Array.Empty<TopicStatisticsDto>();
}

public class TopicStatisticsDto
{
    public long? TopicId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public int AttemptsTotal { get; set; }
    public int CorrectTotal { get; set; }
    public int ErrorsCount { get; set; }
    public decimal? AccuracyPercentage { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
}

