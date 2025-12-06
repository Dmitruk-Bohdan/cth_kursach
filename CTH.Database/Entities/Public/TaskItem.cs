using System.Text.Json;

namespace CTH.Database.Entities.Public;

public sealed class TaskItem
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public long? TopicId { get; set; }
    public long? ExamSourceId { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public short Difficulty { get; set; }
    public string Statement { get; set; } = string.Empty;
    public JsonDocument CorrectAnswer { get; set; } = null!;
    public string? Explanation { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Subject Subject { get; set; } = null!;
    public Topic? Topic { get; set; }
    public ExamSource? ExamSource { get; set; }
    public ICollection<TestTask> TestTasks { get; set; } = new List<TestTask>();
    public ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}
