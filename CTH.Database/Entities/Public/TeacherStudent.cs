namespace CTH.Database.Entities.Public;

public sealed class TeacherStudent
{
    public long Id { get; set; }
    public long TeacherId { get; set; }
    public long StudentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? EstablishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public UserAccount Teacher { get; set; } = null!;
    public UserAccount Student { get; set; } = null!;
}
