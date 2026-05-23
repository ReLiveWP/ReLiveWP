using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Duende.IdentityModel.Jwk;
using Duende.IdentityModel.OidcClient.DPoP;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.Identity;
using ReLiveWP.Backend.Identity.Certificates;
using ReLiveWP.Backend.Identity.ConnectedServices;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Backend.Identity.Grpc;
using ReLiveWP.Backend.Identity.Services;

using ServiceCaps = ReLiveWP.Identity.Data.LiveConnectedServiceCapabilities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("AtProtoClient", (c) =>
{
    c.DefaultRequestHeaders.Add("User-Agent", "ReLiveWP/1.0 (+https://github.com/ReLiveWP/ReLiveWP)");
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LiveDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddIdentity<LiveUser, LiveRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 1;
})
.AddEntityFrameworkStores<LiveDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidIssuers = ["https://relivewp.net/"],
        ValidateAudience = true,
        ValidAudiences = ["http://Passport.NET/tb", "relivewp.net", "spaces.int.relivewp.net", "spaces.relivewp.net"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]!))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<TokenManager>();
builder.Services.AddScoped<LiveIdDeviceCertificateService>();
builder.Services.AddScoped<RootCACertificateProvider>();

builder.Services.AddSingleton<ServiceTokenLocks>();
builder.Services.AddScoped<IClientAssertionService, ClientAssertionService>();
builder.Services.AddScoped<IJWKProvider, JWKProvider>();
builder.Services.AddScoped<AtProtoOAuthProvider>();

builder.Services.AddConnectedServices()
    .AddConnectedService((s) =>
    {
        return new()
        {
            ServiceId = "atproto",
            DisplayName = "AtProto",
            ClientId = builder.Configuration["ConnectedServices:AtProto:ClientId"]!,
            RedirectUri = builder.Configuration["ConnectedServices:AtProto:RedirectUrl"]!,
            Scopes = "atproto transition:generic",
            ServiceCapabilities = ServiceCaps.SocialFeed | ServiceCaps.SocialCheckIn | ServiceCaps.SocialNotifications | ServiceCaps.SocialPost,
            OAuthHandler = (s) =>
            {
                return Task.FromResult<IOAuthProvider>(s.GetRequiredService<AtProtoOAuthProvider>());
            }
        };
    });

builder.Services.AddGrpc();

builder.Services.AddHostedService<TokenRefreshService>();

var app = builder.Build();

ApplyMigrations(app);

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
app.MapGrpcService<AuthenticationService>();
app.MapGrpcService<UserService>();
app.MapGrpcService<ConnectedAccountsService>();

AtProtoProxy.Map(app);

app.Run();

static void ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    using var dbContext = scope.ServiceProvider.GetRequiredService<LiveDbContext>();

    dbContext.Database.Migrate();

    if (dbContext.DPoPKeys.Count() == 0)
    {
        for (int i = 0; i < 1; i++)
        {
            var keyId = $"Key{i}";
            var jwk = JsonWebKeys.CreateECDsa("ES256");
            jwk.KeyId = keyId;

            dbContext.DPoPKeys.Add(new LiveDPoPKey() { Id = keyId, Key = JsonSerializer.Serialize(jwk) });
        }

        dbContext.SaveChanges();
    }
}