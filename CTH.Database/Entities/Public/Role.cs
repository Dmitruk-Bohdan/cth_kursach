namespace CTH.Database.Entities.Public;

public sealed class Role
{
    public long Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<UserAccount> Users { get; set; } = new List<UserAccount>();
}
