namespace CTH.Database.Entities.Public;

public sealed class UserStats
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long? SubjectId { get; set; }
    public long? TopicId { get; set; }
    public int AttemptsTotal { get; set; }
    public int CorrectTotal { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public decimal? AverageScore { get; set; }
    public int? AverageTimeSec { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public UserAccount User { get; set; } = null!;
    public Subject? Subject { get; set; }
    public Topic? Topic { get; set; }
}
