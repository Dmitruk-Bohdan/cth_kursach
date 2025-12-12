namespace CTH.Database.Entities.Public;

public sealed class Subject
{
    public long Id { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<Test> Tests { get; set; } = new List<Test>();
    public ICollection<UserStats> UserStats { get; set; } = new List<UserStats>();
}
