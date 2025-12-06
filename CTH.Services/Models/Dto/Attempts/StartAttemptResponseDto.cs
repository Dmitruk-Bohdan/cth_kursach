namespace CTH.Services.Models.Dto.Attempts;

public class StartAttemptResponseDto
{
    public long AttemptId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
