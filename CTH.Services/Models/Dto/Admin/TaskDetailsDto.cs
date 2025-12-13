namespace CTH.Services.Models.Dto.Admin;

public sealed class TaskDetailsDto
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public long? TopicId { get; set; }
    public string? TopicName { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public short Difficulty { get; set; }
    public string Statement { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

