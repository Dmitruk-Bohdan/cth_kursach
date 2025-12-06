namespace CTH.Database.Entities.Public;

public sealed class Test
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string TestKind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public long? AuthorId { get; set; }
    public int? TimeLimitSec { get; set; }
    public short? AttemptsAllowed { get; set; }
    public string? Mode { get; set; }
    public bool IsPublished { get; set; }
    public bool IsPublic { get; set; }
    public bool IsStateArchive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Subject Subject { get; set; } = null!;
    public UserAccount? Author { get; set; }
    public ICollection<TestTask> TestTasks { get; set; } = new List<TestTask>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
}
