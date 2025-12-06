using CTH.MobileClient;

var baseUrl = Environment.GetEnvironmentVariable("CTH_API_BASE_URL") ?? "https://localhost:7008";
Console.WriteLine($"Using API base URL: {baseUrl}");

using var app = new MobileClientApp(baseUrl);
await app.RunAsync();
