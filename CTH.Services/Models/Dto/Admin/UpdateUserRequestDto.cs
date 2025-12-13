namespace CTH.Services.Models.Dto.Admin;

public sealed class UpdateUserRequestDto
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public int? RoleTypeId { get; set; }
}

