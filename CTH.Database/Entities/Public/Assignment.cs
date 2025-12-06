namespace CTH.Database.Entities.Public;

public sealed class Assignment
{
    public long Id { get; set; }
    public long TestId { get; set; }
    public long TeacherId { get; set; }
    public long StudentId { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public short? AttemptsAllowed { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Test Test { get; set; } = null!;
    public UserAccount Teacher { get; set; } = null!;
    public UserAccount Student { get; set; } = null!;
    public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
}
