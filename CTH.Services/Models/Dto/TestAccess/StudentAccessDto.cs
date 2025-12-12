namespace CTH.Services.Models.Dto.TestAccess;

public class StudentAccessDto
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

