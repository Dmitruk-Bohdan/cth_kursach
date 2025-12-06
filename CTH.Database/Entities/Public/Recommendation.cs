namespace CTH.Database.Entities.Public;

public sealed class Recommendation
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long SubjectId { get; set; }
    public long TopicId { get; set; }
    public short Priority { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public UserAccount User { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public Topic Topic { get; set; } = null!;
}
