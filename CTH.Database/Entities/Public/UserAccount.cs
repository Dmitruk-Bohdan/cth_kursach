namespace CTH.Database.Entities.Public;

public sealed class UserAccount
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleTypeId { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Role Role { get; set; } = null!;
    public ICollection<TeacherStudent> TeacherRelations { get; set; } = new List<TeacherStudent>();
    public ICollection<TeacherStudent> StudentRelations { get; set; } = new List<TeacherStudent>();
    public ICollection<Test> AuthoredTests { get; set; } = new List<Test>();
    public ICollection<Assignment> AssignmentsAsTeacher { get; set; } = new List<Assignment>();
    public ICollection<Assignment> AssignmentsAsStudent { get; set; } = new List<Assignment>();
    public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
    public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<UserStats> UserStats { get; set; } = new List<UserStats>();
}
