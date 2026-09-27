using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.Identity.Certificates;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Backend.Identity.Grpc;
using ReLiveWP.Backend.Identity.Services;
using ReLiveWP.Identity.LiveID;
using ReLiveWP.ServiceDefaults.Media;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LiveDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddReadinessCheck("postgres", (sp, ct) => sp.GetRequiredService<LiveDbContext>().Database.CanConnectAsync(ct));
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddIdentity<LiveUser, LiveRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.User.RequireUniqueEmail = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 1;
})
.AddEntityFrameworkStores<LiveDbContext>()
.AddDefaultTokenProviders()
.AddPasswordValidator<UserPasswordValidator>();

builder.Services.AddScoped<TokenManager>();
builder.Services.AddScoped<UserRegistrationService>();
AddInviteCodeValidator(builder);
builder.Services.AddScoped<SsoSessionManager>();
builder.Services.AddSingleton<ISsoAuthorizationCodeStore, RedisSsoAuthorizationCodeStore>();
builder.Services.AddScoped<LiveIdDeviceCertificateService>();
builder.Services.AddScoped<RootCACertificateProvider>();
builder.Services.AddSingleton<AvatarStore>();
builder.AddMediaPipelineClient();
builder.Services.AddSingleton<AvatarProcessor>();

builder.Services.AddGrpc();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<LiveDbContext>().Database.Migrate();
}

if (JwtKeyLoader.GetVerifyingKey(app.Configuration) is ECDsaSecurityKey publicKey)
{
    app.Services.GetRequiredService<ILogger<Program>>().LogInformation(
        "JWT ES256 public key (JWT:PublicKey): {PublicKey}",
        Convert.ToBase64String(publicKey.ECDsa.ExportSubjectPublicKeyInfo()));
}

app.MapGrpcService<AuthenticationService>();
app.MapGrpcService<UserService>();
app.MapGrpcService<SsoService>();

app.MapDefaultEndpoints();

app.Run();

static void AddInviteCodeValidator(WebApplicationBuilder builder)
{
    if (!builder.Configuration.GetValue<bool>("InviteKeys:Required"))
    {
        builder.Services.AddSingleton<IInviteCodeValidator, PermissiveInviteCodeValidator>();
        return;
    }

    var inviteCodeValidator = new ProductKeyInviteCodeValidator(builder.Configuration);
    builder.Services.AddSingleton<IInviteCodeValidator>(inviteCodeValidator);
}
