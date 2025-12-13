namespace CTH.Services.Models.Dto.Tasks;

public sealed class UpdateTaskRequestDto
{
    public long? TopicId { get; set; }
    public string? TaskType { get; set; }
    public short? Difficulty { get; set; }
    public string? Statement { get; set; }
    public string? CorrectAnswer { get; set; }
    public string? Explanation { get; set; }
    public bool? IsActive { get; set; }
}

