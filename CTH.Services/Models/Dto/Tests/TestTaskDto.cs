namespace CTH.Services.Models.Dto.Tests;

public class TestTaskDto
{
    public long TaskId { get; set; }
    public int Position { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string Statement { get; set; } = string.Empty;
    public short Difficulty { get; set; }
    public string? Explanation { get; set; }
}
