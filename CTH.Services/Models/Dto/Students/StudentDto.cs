namespace CTH.Services.Models.Dto.Students;

public class StudentDto
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset? EstablishedAt { get; set; }
}

