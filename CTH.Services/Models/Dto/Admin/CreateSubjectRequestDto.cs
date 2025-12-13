namespace CTH.Services.Models.Dto.Admin;

public sealed class CreateSubjectRequestDto
{
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

