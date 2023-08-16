using Microsoft.AspNetCore.Mvc.Formatters;
using Zune.Net.Shared;
using ReLiveWP.Backend.Identity;
using ReLiveWP.Backend.ClientProvisioning;
using ReLiveWP.Backend.Certificates;
using ReLiveWP.Backend.Identity.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ReLiveWP.Services.Authentication.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews(o =>
{
    // modCheck WithDefaultOutputFormatter? no?
    o.OutputFormatters.Insert(0, new XmlSerializerOutputFormatter());
    o.InputFormatters.Insert(0, new ZestInputFormatter(o));
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LiveDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddIdentity<LiveUser, LiveRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
})
                .AddEntityFrameworkStores<LiveDbContext>()
                .AddDefaultTokenProviders();

builder.Services.AddAuthentication((o) =>
{
    o.DefaultAuthenticateScheme = "WindowsLiveID";
    o.DefaultChallengeScheme = "WindowsLiveID";
    o.DefaultForbidScheme = "WindowsLiveID";
    o.DefaultScheme = "WindowsLiveID";
})
    .AddScheme<LiveIDAuthOptions, LiveIdAuthHandler>("WindowsLiveID", (options) =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
        };
    });


builder.Services.AddGrpcClient<ClientProvisioning.ClientProvisioningClient>(
    o => o.Address = new Uri("https://localhost:5001"));

builder.Services.AddSingleton<RootCACertificateProvider>();

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints => endpoints.MapControllers());

app.Run();
