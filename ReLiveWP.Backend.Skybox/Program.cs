using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Skybox.Data;
using ReLiveWP.Backend.Skybox.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SkyDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

ApplyMigrations(app);

// Configure the HTTP request pipeline.
app.MapGrpcService<SkyboxDeviceService>();

app.Run();

static void ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    using var dbContext = scope.ServiceProvider.GetRequiredService<SkyDbContext>();

    dbContext.Database.Migrate();
}