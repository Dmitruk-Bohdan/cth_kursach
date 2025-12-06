namespace CTH.Services.Models.Dto.Tests;

public class UpdateTestRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string TestKind { get; set; } = string.Empty;
    public long SubjectId { get; set; }
    public int? TimeLimitSec { get; set; }
    public short? AttemptsAllowed { get; set; }
    public string? Mode { get; set; }
    public bool IsPublished { get; set; }
    public bool IsPublic { get; set; }
    public bool IsStateArchive { get; set; }
    public IReadOnlyCollection<TestTaskUpdateDto> Tasks { get; set; } = Array.Empty<TestTaskUpdateDto>();
}
