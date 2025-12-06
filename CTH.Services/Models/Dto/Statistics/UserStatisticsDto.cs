namespace CTH.Services.Models.Dto.Statistics;

public class UserStatisticsDto
{
    public long Id { get; set; }
    public long? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public long? TopicId { get; set; }
    public string? TopicName { get; set; }
    public int AttemptsTotal { get; set; }
    public int CorrectTotal { get; set; }
    public decimal? AccuracyPercentage { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public decimal? AverageScore { get; set; }
    public int? AverageTimeSec { get; set; }
}

