using CTH.Api.Extensions;
using CTH.Services.Settings;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

var configuration = builder.Configuration;
var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();

builder.Services
    .ConfigureApiLayer(builder.Configuration);

if (!appSettings!.IsProduction)
{
    builder.Services
        .ConfigureSwagger();
}

var app = builder.Build();

if (!appSettings!.IsProduction)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(appSettings!.IsLocal ? "DevSiteCorsPolicy" : "ProdSiteCorsPolicy");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

app.Run();
