using CTH.Services.Models.Dto.Tests;

namespace CTH.Services.Models.Dto.Attempts;

public class AttemptDetailsWithTasksDto
{
    public long Id { get; set; }
    public long TestId { get; set; }
    public string TestTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public decimal? RawScore { get; set; }
    public decimal? ScaledScore { get; set; }
    public int? DurationSec { get; set; }
    public IReadOnlyCollection<AttemptTaskDetailsDto> Tasks { get; set; } = Array.Empty<AttemptTaskDetailsDto>();
}

public class AttemptTaskDetailsDto
{
    public long TaskId { get; set; }
    public int Position { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string Statement { get; set; } = string.Empty;
    public short Difficulty { get; set; }
    public string? Explanation { get; set; }
    public string? CorrectAnswer { get; set; }
    public string? GivenAnswer { get; set; }
    public bool? IsCorrect { get; set; }
    public int? TimeSpentSec { get; set; }
}

