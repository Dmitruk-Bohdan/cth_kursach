namespace CTH.Database.Entities.Public;

public sealed class Topic
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public string? TopicCode { get; set; }
    public long? TopicParentId { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Subject Subject { get; set; } = null!;
    public Topic? Parent { get; set; }
    public ICollection<Topic> Children { get; set; } = new List<Topic>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<UserStats> UserStats { get; set; } = new List<UserStats>();
    public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
}
