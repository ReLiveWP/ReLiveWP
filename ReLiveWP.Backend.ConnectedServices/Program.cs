using System.Text.Json;
using Duende.IdentityModel.OidcClient.DPoP;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ConnectedServices;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Grpc;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;
using ReLiveWP.Backend.ConnectedServices.Proxy;
using ReLiveWP.Backend.ConnectedServices.Services;
using ReLiveWP.Identity;

using ServiceCaps = ReLiveWP.Backend.ConnectedServices.Data.LiveConnectedServiceCapabilities;
using GoogleService = ReLiveWP.Backend.ConnectedServices.OAuthProviders.Google;
using MicrosoftService = ReLiveWP.Backend.ConnectedServices.OAuthProviders.Microsoft;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddHttpClient("AtProtoClient", c =>
{
    c.DefaultRequestHeaders.Add("User-Agent", "ReLiveWP/1.0 (+https://github.com/ReLiveWP/ReLiveWP)");
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ConnectedServicesDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddLiveIDAuthentication(o =>
{
    o.IdentityGrpcConfiguration = c => c.Address = new Uri(builder.Configuration["Endpoints:Identity"]!);
    o.LiveIDConfiguration = c => c.ValidServiceTargets =
    [
        "http://Passport.NET/tb",
        "relivewp.net",
        "spaces.int.relivewp.net",
        "spaces.relivewp.net",
        "skydrive.int.relivewp.com", // oops! 
        "skydrive.relivewp.com", // oops!
        "skydrive.int.relivewp.net",
        "skydrive.relivewp.net",
    ];
});

builder.Services.AddAuthorization();

builder.Services.AddSingleton<ServiceTokenLocks>();
builder.Services.AddScoped<IClientAssertionService, ClientAssertionService>();
builder.Services.AddScoped<IJWKProvider, JWKProvider>();
builder.Services.AddScoped<AtProtoOAuthProvider>();
builder.Services.AddScoped<GoogleOAuthProvider>();
builder.Services.AddScoped<MicrosoftOAuthProvider>();

builder.Services.AddScoped<IConnectedServiceProxy, AtProtoServiceProxy>();
builder.Services.AddScoped<IConnectedServiceProxy, GoogleServiceProxy>();

builder.Services.AddConnectedServices()
    .AddConnectedService(s => new()
    {
        ServiceId = AtProto.SERVICE_NAME,
        DisplayName = "AtProto",
        ClientId = builder.Configuration["ConnectedServices:AtProto:ClientId"]!,
        RedirectUri = builder.Configuration["ConnectedServices:AtProto:RedirectUrl"]!,
        Scopes = "atproto transition:generic",
        ServiceCapabilities = ServiceCaps.SocialFeed | ServiceCaps.SocialCheckIn | ServiceCaps.SocialNotifications | ServiceCaps.SocialPost,
        OAuthHandler = s => Task.FromResult<IOAuthProvider>(s.GetRequiredService<AtProtoOAuthProvider>())
    })
    .AddConnectedService(s => new()
    {
        ServiceId = GoogleService.SERVICE_NAME,
        DisplayName = "Google",
        ClientId = builder.Configuration["ConnectedServices:Google:ClientId"]!,
        ClientSecret = builder.Configuration["ConnectedServices:Google:ClientSecret"]!,
        RedirectUri = builder.Configuration["ConnectedServices:Google:RedirectUrl"]!,
        Scopes = string.Concat("openid ",
            "https://www.googleapis.com/auth/userinfo.profile ",
            "https://www.googleapis.com/auth/userinfo.email ",
            // "https://www.googleapis.com/auth/contacts ",
            // "https://www.googleapis.com/auth/calendar ",
            // "https://www.googleapis.com/auth/calendar.events ",
            // "https://www.googleapis.com/auth/drive ",
            // "https://www.googleapis.com/auth/gmail.modify ",
            "https://www.googleapis.com/auth/photoslibrary.appendonly ",
            "https://www.googleapis.com/auth/photoslibrary.edit.appcreateddata "),
        ServiceCapabilities = 
            // ServiceCaps.Email | 
            // ServiceCaps.Contacts | 
            // ServiceCaps.Calendar | 
            // ServiceCaps.FileStorage |
            ServiceCaps.PhotoSync,
        OAuthHandler = s => Task.FromResult<IOAuthProvider>(s.GetRequiredService<GoogleOAuthProvider>())
    })
    .AddConnectedService(s => new()
    {
        ServiceId = MicrosoftService.SERVICE_NAME,
        DisplayName = "Microsoft",
        ClientId = builder.Configuration["ConnectedServices:Microsoft:ClientId"]!,
        ClientSecret = builder.Configuration["ConnectedServices:Microsoft:ClientSecret"]!,
        RedirectUri = builder.Configuration["ConnectedServices:Microsoft:RedirectUrl"]!,
        Scopes = "openid profile email offline_access  https://graph.microsoft.com/User.Read https://graph.microsoft.com/Files.ReadWrite.AppFolder",
        ServiceCapabilities = ServiceCaps.FileStorage | ServiceCaps.PhotoSync,
        OAuthHandler = s => Task.FromResult<IOAuthProvider>(s.GetRequiredService<MicrosoftOAuthProvider>())
    });

builder.Services.AddGrpc();
builder.Services.AddHostedService<TokenRefreshService>();

var app = builder.Build();

ApplyMigrations(app);

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<ConnectedAccountsService>();

app.MapConnectedServicesProxy();

app.Run();

static void ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    using var dbContext = scope.ServiceProvider.GetRequiredService<ConnectedServicesDbContext>();

    dbContext.Database.Migrate();

    if (!dbContext.DPoPKeys.Any())
    {
        var keyId = "Key0";
        var jwk = JsonWebKeys.CreateECDsa("ES256");
        jwk.KeyId = keyId;
        dbContext.DPoPKeys.Add(new LiveDPoPKey() { Id = keyId, Key = JsonSerializer.Serialize(jwk) });
    }

    foreach (var service in dbContext.ConnectedServices)
    {
        // migrating available capabilities from enabled capabilities for now, this is fine
        // because users cant yet modify caps
        if (service.AvailableCapabilities == 0)
            service.AvailableCapabilities = service.EnabledCapabilities;
    }

    dbContext.SaveChanges();
}
