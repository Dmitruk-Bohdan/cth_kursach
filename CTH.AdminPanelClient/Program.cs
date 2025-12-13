using CTH.AdminPanelClient;

const string baseUrl = "https://localhost:7008";

var app = new AdminPanelClientApp(baseUrl);
await app.RunAsync();
