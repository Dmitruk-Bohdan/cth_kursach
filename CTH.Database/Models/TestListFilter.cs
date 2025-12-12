namespace CTH.Database.Models;

public class TestListFilter
{
    public long? SubjectId { get; set; }
    public bool OnlyTeachers { get; set; }
    public bool OnlyStateArchive { get; set; }
    public bool OnlyLimitedAttempts { get; set; }
    public string? TitlePattern { get; set; }
    public string? Mode { get; set; }
}

