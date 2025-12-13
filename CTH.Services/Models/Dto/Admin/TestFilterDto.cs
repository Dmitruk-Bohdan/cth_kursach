namespace CTH.Services.Models.Dto.Admin;

public sealed class TestFilterDto
{
    public long? SubjectId { get; set; }
    public string? TestKind { get; set; }
    public long? AuthorId { get; set; }
    public bool? IsPublished { get; set; }
    public bool? IsStateArchive { get; set; }
    public string? Search { get; set; }
}

