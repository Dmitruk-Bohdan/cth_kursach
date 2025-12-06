using System.Text.Json;

namespace CTH.Database.Entities.Public;

public sealed class UserAnswer
{
    public long Id { get; set; }
    public long AttemptId { get; set; }
    public long TaskId { get; set; }
    public JsonDocument GivenAnswer { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public int? TimeSpentSec { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Attempt Attempt { get; set; } = null!;
    public TaskItem Task { get; set; } = null!;
}
