using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

var app = builder.Build();

//app.UseHttpsRedirection();

app.MapGet("/config", (IWebHostEnvironment environment) =>
{
    return Results.File(Path.Join(environment.WebRootPath, "relivewp_config.db"), "application/x-sqlite3", "ppcrlconfig.db");
});

app.MapGet("/config_int", (IWebHostEnvironment environment) =>
{
    return Results.File(Path.Join(environment.WebRootPath, "relivewp_config_int.db"), "application/x-sqlite3", "ppcrlconfig.db");
});

app.MapGet("/config/version", () =>
{
    return new ClientConfigVersions(2, 2);
});

app.Run();

internal record ClientConfigVersions(int MinVersion, int CurrentVersion);