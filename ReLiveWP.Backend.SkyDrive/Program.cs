using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.SkyDrive.Data;
using ReLiveWP.Backend.SkyDrive.Services;
using ReLiveWP.Services.Grpc;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

// Add services to the container.
builder.Services.AddGrpc();

builder.Services.AddHttpClient();
builder.Services.AddGrpcClient<ConnectedServices.ConnectedServicesClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:ConnectedServices:Grpc"]!));

builder.Services.AddScoped<IPhotoSyncProxyClient, GooglePhotosProxyClient>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SkyDriveDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

await ApplyMigrations(app);

// Configure the HTTP request pipeline.
app.MapGrpcService<SkyDriveService>();

app.Run();

static async Task ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    using var dbContext = scope.ServiceProvider.GetRequiredService<SkyDriveDbContext>();

    await dbContext.Database.MigrateAsync();
}
