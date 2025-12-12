namespace CTH.Services.Models.Dto.Invitations;

public class InvitationCodeDto
{
    public long Id { get; set; }
    public long TeacherId { get; set; }
    public string Code { get; set; } = string.Empty;
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

