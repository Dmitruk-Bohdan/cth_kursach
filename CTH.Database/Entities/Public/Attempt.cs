namespace CTH.Database.Entities.Public;

public sealed class Attempt
{
    public long Id { get; set; }
    public long TestId { get; set; }
    public long UserId { get; set; }
    public long? AssignmentId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? RawScore { get; set; }
    public decimal? ScaledScore { get; set; }
    public int? DurationSec { get; set; }
    public long? Seed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Test Test { get; set; } = null!;
    public UserAccount User { get; set; } = null!;
    public Assignment? Assignment { get; set; }
    public ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}
