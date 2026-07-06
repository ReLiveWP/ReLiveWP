using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Skybox.Data;
using ReLiveWP.Backend.Skybox.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddHttpClient();

builder.Services.AddScoped<DeviceCommandService>();

builder.Services.AddRedis(builder.Configuration);
builder.Services.AddSingleton<CommandRegistry>();
builder.Services.AddSingleton<CommandStatusHub>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SkyDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

await ApplyMigrations(app);

// Configure the HTTP request pipeline.
app.MapGrpcService<SkyboxDeviceService>();

app.MapDefaultEndpoints();

app.Run();

static async Task ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    using var dbContext = scope.ServiceProvider.GetRequiredService<SkyDbContext>();

    dbContext.Database.Migrate();
}