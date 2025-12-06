namespace CTH.Services.Models.Dto.Attempts;

public class CompleteAttemptRequestDto
{
    public decimal? RawScore { get; set; }
    public decimal? ScaledScore { get; set; }
    public int? DurationSec { get; set; }
}
