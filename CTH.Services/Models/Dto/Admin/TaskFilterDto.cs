namespace CTH.Services.Models.Dto.Admin;

public sealed class TaskFilterDto
{
    public long? SubjectId { get; set; }
    public long? TopicId { get; set; }
    public string? TaskType { get; set; }
    public short? Difficulty { get; set; }
    public bool? IsActive { get; set; }
    public string? Search { get; set; }
}

