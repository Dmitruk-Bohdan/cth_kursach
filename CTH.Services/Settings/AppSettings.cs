namespace CTH.Services.Settings;

public class AppSettings
{
    public string Environment { get; set; } = null!;
    public bool IsProduction => Environment.Equals("Prod", StringComparison.OrdinalIgnoreCase);
    public bool IsLocal => Environment.Equals("Local", StringComparison.OrdinalIgnoreCase);
}