namespace CTH.Services.Models.Dto.Admin;

public sealed class UpdateSubjectRequestDto
{
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
    public bool? IsActive { get; set; }
}

