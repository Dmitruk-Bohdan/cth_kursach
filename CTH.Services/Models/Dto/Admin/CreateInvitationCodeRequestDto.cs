namespace CTH.Services.Models.Dto.Admin;

public sealed class CreateInvitationCodeRequestDto
{
    public long TeacherId { get; set; }
    public int? MaxUses { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}




