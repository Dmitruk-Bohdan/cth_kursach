namespace CTH.Services.Models.Dto.Admin;

public sealed class ExamSourceDetailsDto
{
    public long Id { get; set; }
    public int Year { get; set; }
    public int? VariantNumber { get; set; }
    public string? Issuer { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

