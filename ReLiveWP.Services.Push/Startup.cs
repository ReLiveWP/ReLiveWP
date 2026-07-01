using Microsoft.EntityFrameworkCore;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Push.Data;
using ReLiveWP.Services.Push.Nsp;
using ReLiveWP.Services.Push.Services;

namespace ReLiveWP.Services.Push;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews();
        services.AddHttpClient();

        services.AddHostedService<PushTcpService>();

        services.AddDbContextFactory<PushDatabase>(
            o => o.UseNpgsql(Configuration.GetConnectionString("Push") ?? "Host=localhost;Database=relive_push;Username=relive;Password=relive"));

        services.AddScoped<ChannelStore>();
        services.AddScoped<NotificationQueue>();
        services.AddScoped<SessionStore>();

        // presence is shared across every connection scope and the trigger's request scope
        services.AddSingleton<PushPresence>();

        services.AddHostedService<NotificationCleanupService>();

        services.AddGrpcClient<Authentication.AuthenticationClient>(
            o => o.Address = new Uri(Configuration["Endpoints:Identity"]!));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        using (var scope = app.ApplicationServices.CreateScope())
        {
            using var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PushDatabase>>().CreateDbContext();
            db.Database.Migrate();
        }

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
