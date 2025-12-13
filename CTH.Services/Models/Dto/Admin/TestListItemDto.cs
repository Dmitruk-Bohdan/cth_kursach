namespace CTH.Services.Models.Dto.Admin;

public sealed class TestListItemDto
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string TestKind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public long? AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public bool IsPublished { get; set; }
    public bool IsPublic { get; set; }
    public bool IsStateArchive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

