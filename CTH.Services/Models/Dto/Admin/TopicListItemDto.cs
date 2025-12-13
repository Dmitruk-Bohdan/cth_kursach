namespace CTH.Services.Models.Dto.Admin;

public sealed class TopicListItemDto
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public string? TopicCode { get; set; }
    public long? TopicParentId { get; set; }
    public string? ParentTopicName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

