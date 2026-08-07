using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.SkyDrive.Data;
using ReLiveWP.Backend.SkyDrive.Services;
using ReLiveWP.Identity;
using ReLiveWP.Identity.Grpc;
using ReLiveWP.Services.Grpc;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddGrpc();

builder.Services.AddGrpcAuthentication();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AuthForwardingInterceptor>();

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddGrpcClient<ConnectedServices.ConnectedServicesClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:ConnectedServices:Grpc"]!))
    .AddInterceptor<AuthForwardingInterceptor>();

builder.Services.AddScoped<IPhotoSyncProxyClient, GooglePhotosProxyClient>();
builder.Services.AddScoped<IPhotoSyncProxyClient, OneDrivePhotoSyncProxyClient>();

builder.Services.AddScoped<IFileSyncProxyClient, OneDriveFileSyncProxyClient>();

builder.Services.AddRedis(builder.Configuration);
builder.Services.AddSingleton<SyncCursorStore>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SkyDriveDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

await ApplyMigrations(app);

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<SkyDriveService>();
app.MapGrpcService<SkyDocsService>();

app.MapDefaultEndpoints();

app.Run();

static async Task ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    using var dbContext = scope.ServiceProvider.GetRequiredService<SkyDriveDbContext>();

    await dbContext.Database.MigrateAsync();
}
