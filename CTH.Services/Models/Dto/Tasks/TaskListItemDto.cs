namespace CTH.Services.Models.Dto.Tasks;

public sealed class TaskListItemDto
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public long? TopicId { get; set; }
    public string? TopicName { get; set; }
    public string? TopicCode { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public short Difficulty { get; set; }
    public string Statement { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string? CorrectAnswer { get; set; }
}

