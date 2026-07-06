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

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LiveDbContext>(options => options.UseNpgsql(connectionString));
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

builder.Services.AddScoped<TokenManager>();
builder.Services.AddScoped<LiveIdDeviceCertificateService>();
builder.Services.AddScoped<RootCACertificateProvider>();

builder.Services.AddGrpc();

builder.Services.AddHostedService<UserMigrationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<LiveDbContext>().Database.Migrate();
}

app.MapGrpcService<AuthenticationService>();
app.MapGrpcService<UserService>();

app.MapDefaultEndpoints();

app.Run();
