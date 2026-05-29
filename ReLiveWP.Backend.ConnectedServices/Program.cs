using System.Text;
using System.Text.Json;
using Duende.IdentityModel.Jwk;
using Duende.IdentityModel.OidcClient.DPoP;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.ConnectedServices;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Grpc;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;
using ReLiveWP.Backend.ConnectedServices.Proxy;
using ReLiveWP.Backend.ConnectedServices.Services;
using ReLiveWP.Identity;

using ServiceCaps = ReLiveWP.Backend.ConnectedServices.Data.LiveConnectedServiceCapabilities;

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
        "spaces.relivewp.net"
    ];
});

builder.Services.AddAuthorization();

builder.Services.AddSingleton<ServiceTokenLocks>();
builder.Services.AddScoped<IClientAssertionService, ClientAssertionService>();
builder.Services.AddScoped<IJWKProvider, JWKProvider>();
builder.Services.AddScoped<AtProtoOAuthProvider>();

builder.Services.AddScoped<IConnectedServiceProxy, AtProtoServiceProxy>();

builder.Services.AddConnectedServices()
    .AddConnectedService(s => new()
    {
        ServiceId = "atproto",
        DisplayName = "AtProto",
        ClientId = builder.Configuration["ConnectedServices:AtProto:ClientId"]!,
        RedirectUri = builder.Configuration["ConnectedServices:AtProto:RedirectUrl"]!,
        Scopes = "atproto transition:generic",
        ServiceCapabilities = ServiceCaps.SocialFeed | ServiceCaps.SocialCheckIn | ServiceCaps.SocialNotifications | ServiceCaps.SocialPost,
        OAuthHandler = s => Task.FromResult<IOAuthProvider>(s.GetRequiredService<AtProtoOAuthProvider>())
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
        dbContext.SaveChanges();
    }
}
