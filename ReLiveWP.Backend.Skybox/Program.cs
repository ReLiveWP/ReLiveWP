using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Skybox.Data;
using ReLiveWP.Backend.Skybox.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddHttpClient();

builder.Services.AddScoped<DeviceCommandService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SkyDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

await ApplyMigrations(app);

// Configure the HTTP request pipeline.
app.MapGrpcService<SkyboxDeviceService>();

app.Run();

static async Task ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    using var dbContext = scope.ServiceProvider.GetRequiredService<SkyDbContext>();

    dbContext.Database.Migrate();
}