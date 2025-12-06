namespace CTH.Services.Models.Dto.Attempts;

public class SubmitAnswerRequestDto
{
    public long TaskId { get; set; }
    public string GivenAnswer { get; set; } = string.Empty;
    public int? TimeSpentSec { get; set; }
}
