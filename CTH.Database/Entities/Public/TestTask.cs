namespace CTH.Database.Entities.Public;

public sealed class TestTask
{
    public long Id { get; set; }
    public long TestId { get; set; }
    public long TaskId { get; set; }
    public int Position { get; set; }
    public decimal? Weight { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Test Test { get; set; } = null!;
    public TaskItem Task { get; set; } = null!;
}
