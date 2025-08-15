using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceRegistration.Data;
using ReLiveWP.Backend.DeviceRegistration.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DevicesDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

ApplyMigrations(app);

// Configure the HTTP request pipeline.
app.MapGrpcService<ClientProvisioningService>();
app.MapGrpcService<DeviceRegistrationService>();

app.Run();

static void ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    using var dbContext = scope.ServiceProvider.GetRequiredService<DevicesDbContext>();

    dbContext.Database.Migrate();
}