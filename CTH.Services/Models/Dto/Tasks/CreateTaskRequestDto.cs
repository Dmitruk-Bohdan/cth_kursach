namespace CTH.Services.Models.Dto.Tasks;

public sealed class CreateTaskRequestDto
{
    public long SubjectId { get; set; }
    public long? TopicId { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public short Difficulty { get; set; }
    public string Statement { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public bool IsActive { get; set; } = true;
}

