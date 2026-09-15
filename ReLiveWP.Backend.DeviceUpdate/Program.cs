using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceUpdate;
using ReLiveWP.Backend.DeviceUpdate.Catalog;
using ReLiveWP.Backend.DeviceUpdate.Commands;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.Configure<CrawlerOptions>(builder.Configuration.GetSection(CrawlerOptions.SectionName));
builder.Services.Configure<PackageOptions>(builder.Configuration.GetSection(PackageOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextPool<UpdatesDbContext>(options => options.UseNpgsql(connectionString));

var upstreamHttp = LongLivedClient("Windows-Update-Agent");
var packagesHttp = LongLivedClient(null);

builder.Services.AddScoped(sp => ActivatorUtilities.CreateInstance<WsusUpstreamClient>(sp, upstreamHttp));
builder.Services.AddScoped(sp => ActivatorUtilities.CreateInstance<PackageStore>(sp, packagesHttp));

builder.Services.AddScoped<UpdateService>();
builder.Services.AddScoped<CatalogReparser>();
builder.Services.AddScoped<CatalogCompiler>();
builder.Services.AddScoped<Crawler>();
builder.Services.AddReadinessCheck("postgres", (sp, ct) => sp.GetRequiredService<UpdatesDbContext>().Database.CanConnectAsync(ct));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<UpdatesDbContext>().Database.MigrateAsync();
}

if (DeviceUpdateCommands.IsCommandInvocation(args))
    return await DeviceUpdateCommands.RunAsync(app, args);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UpdatesDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    CatalogVerifier.LogReport(await CatalogVerifier.CheckCatalogAsync(db), logger);
}

app.MapClientWebService();

await app.RunAsync();
return 0;

static HttpClient LongLivedClient(string? userAgent)
{
    var client = new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(15) })
    {
        Timeout = TimeSpan.FromMinutes(10),
    };

    if (userAgent is not null)
        client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

    return client;
}
