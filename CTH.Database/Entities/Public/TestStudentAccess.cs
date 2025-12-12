namespace CTH.Database.Entities.Public;

public sealed class TestStudentAccess
{
    public long Id { get; set; }
    public long TestId { get; set; }
    public long StudentId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Test Test { get; set; } = null!;
    public UserAccount Student { get; set; } = null!;
}

