namespace CTH.Services.Models.Dto.Attempts;

public class AttemptAnswerDto
{
    public long TaskId { get; set; }
    public string GivenAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int? TimeSpentSec { get; set; }
}
