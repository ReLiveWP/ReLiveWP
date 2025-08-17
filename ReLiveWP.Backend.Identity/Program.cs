using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Backend.Identity.Services;

namespace ReLiveWP.Backend.Identity;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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

        // Add services to the container.
        builder.Services.AddGrpc();

        var app = builder.Build();

        ApplyMigrations(app);

        // Configure the HTTP request pipeline.
        app.MapGrpcService<AuthenticationService>();
        app.MapGrpcService<UserService>();

        app.Run();
    }

    static void ApplyMigrations(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<LiveDbContext>();

        dbContext.Database.Migrate();
    }
}
