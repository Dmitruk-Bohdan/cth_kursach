namespace CTH.Services.Models.Dto.Admin;

public sealed class CreateTopicRequestDto
{
    public long SubjectId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public string? TopicCode { get; set; }
    public long? TopicParentId { get; set; }
    public bool IsActive { get; set; } = true;
}

