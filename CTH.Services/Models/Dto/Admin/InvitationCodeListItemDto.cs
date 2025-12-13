namespace CTH.Services.Models.Dto.Admin;

public sealed class InvitationCodeListItemDto
{
    public long Id { get; set; }
    public long TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string TeacherEmail { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

