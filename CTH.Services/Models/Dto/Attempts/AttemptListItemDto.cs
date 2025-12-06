namespace CTH.Services.Models.Dto.Attempts;

public class AttemptListItemDto
{
    public long Id { get; set; }
    public long TestId { get; set; }
    public string TestTitle { get; set; } = string.Empty;
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? RawScore { get; set; }
}

