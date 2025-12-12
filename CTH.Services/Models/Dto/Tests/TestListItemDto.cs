namespace CTH.Services.Models.Dto.Tests;

public class TestListItemDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TestKind { get; set; } = string.Empty;
    public long SubjectId { get; set; }
    public int? TimeLimitSec { get; set; }
    public short? AttemptsAllowed { get; set; }
    public bool IsPublic { get; set; }
    public bool IsStateArchive { get; set; }
    public string? Mode { get; set; }
}
