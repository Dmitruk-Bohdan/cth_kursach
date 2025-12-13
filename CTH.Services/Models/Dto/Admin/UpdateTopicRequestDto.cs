namespace CTH.Services.Models.Dto.Admin;

public sealed class UpdateTopicRequestDto
{
    public long? SubjectId { get; set; }
    public string? TopicName { get; set; }
    public string? TopicCode { get; set; }
    public long? TopicParentId { get; set; }
    public bool? IsActive { get; set; }
}

