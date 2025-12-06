namespace CTH.Services.Models.Dto.Attempts;

public class AttemptDetailsDto
{
    public long Id { get; set; }
    public long TestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public decimal? RawScore { get; set; }
    public decimal? ScaledScore { get; set; }
    public int? DurationSec { get; set; }
    public IReadOnlyCollection<AttemptAnswerDto> Answers { get; set; } = Array.Empty<AttemptAnswerDto>();
}
