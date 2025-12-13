namespace CTH.Services.Models.Dto.Admin;

public sealed class UpdateExamSourceRequestDto
{
    public int? Year { get; set; }
    public int? VariantNumber { get; set; }
    public string? Issuer { get; set; }
    public string? Notes { get; set; }
}

