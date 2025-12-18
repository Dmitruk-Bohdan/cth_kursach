using CTH.TeacherWebClient;

const string baseUrl = "https://localhost:7008"; 

var app = new TeacherWebClientApp(baseUrl);
await app.RunAsync();

