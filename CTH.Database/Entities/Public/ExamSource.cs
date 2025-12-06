namespace CTH.Database.Entities.Public;

public sealed class ExamSource
{
    public long Id { get; set; }
    public int Year { get; set; }
    public int? VariantNumber { get; set; }
    public string? Issuer { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
