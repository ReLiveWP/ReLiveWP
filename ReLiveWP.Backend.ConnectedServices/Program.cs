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
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

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
builder.Services.AddDbContext<ConnectedServicesDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddGrpcAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddRedis(builder.Configuration);
builder.Services.AddSingleton(sp =>
    RedLockFactory.Create([new RedLockMultiplexer(sp.GetRequiredService<IConnectionMultiplexer>())]));

builder.Services.AddSingleton<ServiceTokenLocks>();
builder.Services.AddSingleton<PendingOAuthStore>();
builder.Services.AddSingleton<ConnectionSecretProtector>();
builder.Services.AddSingleton<IOutboundAddressPolicy, PublicOnlyAddressPolicy>();

builder.Services.AddHttpClient(OutboundAddressPolicyExtensions.GuardedClientName)
    .ConfigurePrimaryHttpMessageHandler(s =>
        OutboundAddressPolicyExtensions.CreateGuardedHandler(s.GetRequiredService<IOutboundAddressPolicy>()));

builder.Services.AddScoped<IClientAssertionService, ClientAssertionService>();
builder.Services.AddScoped<IJWKProvider, JWKProvider>();
builder.Services.AddScoped<AtProtoOAuthProvider>();
builder.Services.AddScoped<GoogleOAuthProvider>();
builder.Services.AddScoped<MicrosoftOAuthProvider>();
builder.Services.AddScoped<DavHomeSetDiscovery>();
builder.Services.AddScoped<WebDavCredentialProvider>();
builder.Services.AddScoped<CardDavCredentialProvider>();
builder.Services.AddScoped<CalDavCredentialProvider>();

builder.Services.AddScoped<IConnectedServiceProxy, AtProtoServiceProxy>();
builder.Services.AddScoped<IConnectedServiceProxy, GoogleServiceProxy>();
builder.Services.AddScoped<IConnectedServiceProxy, MicrosoftServiceProxy>();
builder.Services.AddScoped<IConnectedServiceProxy, WebDavServiceProxy>();
builder.Services.AddScoped<IConnectedServiceProxy, CardDavServiceProxy>();
builder.Services.AddScoped<IConnectedServiceProxy, CalDavServiceProxy>();

builder.Services.AddConnectedServices()
    .AddConnectedService(s => new()
    {
        ServiceId = AtProto.SERVICE_NAME,
        DisplayName = "AtProto",
        ClientId = builder.Configuration["ConnectedServices:AtProto:ClientId"]!,
        RedirectUri = builder.Configuration["ConnectedServices:AtProto:RedirectUrl"]!,
        Scopes = "atproto transition:generic",
        ServiceCapabilities = ServiceCaps.SocialFeed | ServiceCaps.SocialCheckIn | ServiceCaps.SocialNotifications | ServiceCaps.SocialPost | ServiceCaps.SocialPhotos,
        ShareableCapabilities = ServiceCaps.SocialFeed | ServiceCaps.SocialPhotos,
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
            "https://www.googleapis.com/auth/contacts.readonly ",
            "https://www.googleapis.com/auth/calendar.readonly ",
            // "https://www.googleapis.com/auth/drive ",
            // "https://www.googleapis.com/auth/gmail.modify ",
            "https://www.googleapis.com/auth/photoslibrary.appendonly ",
            "https://www.googleapis.com/auth/photoslibrary.edit.appcreateddata ",
            "https://www.googleapis.com/auth/photoslibrary.readonly.appcreateddata "),
        ServiceCapabilities = 
            // ServiceCaps.Email | 
            ServiceCaps.Contacts |
            ServiceCaps.Calendar |
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
        Scopes = string.Concat("openid profile email offline_access ",
            "https://graph.microsoft.com/User.Read ",
            "https://graph.microsoft.com/Files.ReadWrite ",
            "https://graph.microsoft.com/Contacts.Read ",
            "https://graph.microsoft.com/Calendars.Read"),
        ServiceCapabilities = ServiceCaps.FileStorage | ServiceCaps.PhotoSync | ServiceCaps.Contacts | ServiceCaps.Calendar,
        OAuthHandler = s => Task.FromResult<IOAuthProvider>(s.GetRequiredService<MicrosoftOAuthProvider>())
    })
    .AddConnectedService(s => new()
    {
        ServiceId = WebDav.SERVICE_NAME,
        DisplayName = "WebDAV",
        LinkMode = ServiceLinkMode.Credentials,
        ServiceCapabilities = ServiceCaps.FileStorage | ServiceCaps.PhotoSync,
        CredentialHandler = s => Task.FromResult<ICredentialLinkProvider>(s.GetRequiredService<WebDavCredentialProvider>())
    })
    .AddConnectedService(s => new()
    {
        ServiceId = CardDav.SERVICE_NAME,
        DisplayName = "CardDAV",
        LinkMode = ServiceLinkMode.Credentials,
        ServiceCapabilities = ServiceCaps.Contacts,
        CredentialHandler = s => Task.FromResult<ICredentialLinkProvider>(s.GetRequiredService<CardDavCredentialProvider>())
    })
    .AddConnectedService(s => new()
    {
        ServiceId = CalDav.SERVICE_NAME,
        DisplayName = "CalDAV",
        LinkMode = ServiceLinkMode.Credentials,
        ServiceCapabilities = ServiceCaps.Calendar,
        CredentialHandler = s => Task.FromResult<ICredentialLinkProvider>(s.GetRequiredService<CalDavCredentialProvider>())
    });

builder.Services.AddGrpc();
builder.Services.AddHostedService<TokenRefreshService>();
builder.Services.AddHostedService<TransientConnectionSweeper>();

var app = builder.Build();

RequireSecretKeyForCredentialServices(app);

ApplyMigrations(app);

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<ConnectedAccountsService>();

app.MapConnectedServicesProxy();

app.MapDefaultEndpoints();

app.Run();

static void RequireSecretKeyForCredentialServices(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var services = scope.ServiceProvider.GetRequiredService<IConnectedServicesContainer>();
    if (!services.Values.Any(s => s.LinkMode == ServiceLinkMode.Credentials))
        return;

    if (!scope.ServiceProvider.GetRequiredService<ConnectionSecretProtector>().IsConfigured)
        throw new InvalidOperationException(
            $"{ConnectionSecretProtector.KeyConfigPath} must be set when a credential-linked service is registered.");
}

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

    var services = scope.ServiceProvider.GetRequiredService<IConnectedServicesContainer>();
    foreach (var service in dbContext.ConnectedServices)
    {
        if (service.AvailableCapabilities == 0)
            service.AvailableCapabilities = service.EnabledCapabilities;

        if (services.TryGetValue(service.Service, out var description))
            service.AvailableCapabilities |= description.ServiceCapabilities;
    }

    dbContext.SaveChanges();
}
