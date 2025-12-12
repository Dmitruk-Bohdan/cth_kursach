namespace CTH.Services.Models.Dto.Tasks;

public sealed class TopicListItemDto
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public string? TopicCode { get; set; }
}

