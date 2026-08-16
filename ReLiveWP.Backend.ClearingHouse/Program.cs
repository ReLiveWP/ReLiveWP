using Microsoft.EntityFrameworkCore;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using ReLiveWP.Backend.ClearingHouse.Data;
using ReLiveWP.Backend.ClearingHouse.Grpc;
using ReLiveWP.Backend.ClearingHouse.Services.ContactSync;
using ReLiveWP.Identity;
using ReLiveWP.Identity.Grpc;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddGrpc();
builder.Services.AddGrpcAuthentication();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AuthForwardingInterceptor>();
builder.Services.AddHttpClient();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ClearingHouseDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddRedis(builder.Configuration);
builder.Services.AddSingleton(sp =>
    RedLockFactory.Create([new RedLockMultiplexer(sp.GetRequiredService<IConnectionMultiplexer>())]));

builder.Services.AddGrpcClient<ConnectedServices.ConnectedServicesClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:ConnectedServices:Grpc"]!))
    .AddInterceptor<AuthForwardingInterceptor>();

builder.Services.AddGrpcClient<MailboxStore.MailboxStoreClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Mailbox"]!));

builder.Services.AddSingleton<ConnectedServicesProxy>();
builder.Services.AddScoped<IContactSyncDriver, GoogleContactSyncDriver>();
builder.Services.AddScoped<IContactSyncDriver, CardDavContactSyncDriver>();
builder.Services.AddScoped<ContactSyncDriverRegistry>();
builder.Services.AddScoped<ContactPhotoService>();
builder.Services.AddScoped<ContactMirrorService>();
builder.Services.AddScoped<ContactSourceDetach>();

builder.Services.AddSingleton<ContactsFolderResolver>();

builder.Services.AddHostedService<ContactMirrorPoller>();
builder.Services.AddHostedService<ConnectionDeletedSubscriber>();

var app = builder.Build();

ApplyMigrations(app);

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<ClearingHouseService>();
app.MapDefaultEndpoints();

app.Run();

static void ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<ClearingHouseDbContext>().Database.Migrate();
}
