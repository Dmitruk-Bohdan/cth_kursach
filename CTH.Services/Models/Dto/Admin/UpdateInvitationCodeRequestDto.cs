namespace CTH.Services.Models.Dto.Admin;

public sealed class UpdateInvitationCodeRequestDto
{
    public int? MaxUses { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? Status { get; set; }
}


