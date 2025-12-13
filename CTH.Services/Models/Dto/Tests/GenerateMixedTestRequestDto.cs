namespace CTH.Services.Models.Dto.Tests;

public class GenerateMixedTestRequestDto
{
    public long SubjectId { get; set; }
    public int? TimeLimitSec { get; set; }
    public string? Title { get; set; }
    public IReadOnlyCollection<TopicSelectionDto> Topics { get; set; } = Array.Empty<TopicSelectionDto>();
}

public class TopicSelectionDto
{
    public long TopicId { get; set; }
    public int TaskCount { get; set; }
    public short DesiredDifficulty { get; set; } // 1-5
}

