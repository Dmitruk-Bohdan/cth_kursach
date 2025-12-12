namespace CTH.Services.Models.Dto.Invitations;

public class CreateInvitationCodeRequestDto
{
    public int? MaxUses { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

