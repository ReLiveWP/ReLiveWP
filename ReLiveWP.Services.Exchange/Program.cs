using System.Threading.RateLimiting;
using Grpc.Net.ClientFactory;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ReLiveWP.Identity;
using ReLiveWP.Identity.Exchange;
using MailClient = ReLiveWP.Services.Grpc.Mail.Mail.MailClient;
using ReLiveWP.Services.Exchange.Middleware;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();
builder.Services.AddRedis(builder.Configuration);

builder.Services.AddExchangeOrLiveIDAuthentication(opts =>
{
    opts.IdentityGrpcConfiguration = o =>
        o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!);

    opts.ExchangeConfiguration = c => c.Events = new ExchangeAuthEvents
    {
        OnChallenge = ctx =>
        {
            if (!string.IsNullOrEmpty(ctx.Context.Request.Headers.Origin))
            {
                ctx.Context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Handled = true;
            }

            return Task.CompletedTask;
        }
    };

    opts.LiveIDConfiguration = c => c.ValidServiceTargets =
        ["http://Passport.NET/tb", "relivewp.net", "sync.relivewp.net", "sync.int.relivewp.net"];
});

builder.Services.AddGrpcClient<User.UserClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));

builder.Services.AddGrpcClient<MailboxStore.MailboxStoreClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Mailbox"]!))
    .ConfigureChannel(ch => ch.MaxReceiveMessageSize = 32 * 1024 * 1024);

builder.Services.AddGrpcClient<MailClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Mail"]!))
    .ConfigureChannel(ch => ch.MaxSendMessageSize = 32 * 1024 * 1024);

builder.Services.AddControllers();

builder.Services.Configure<ForwardedHeadersOptions>(o =>
    o.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor);

builder.Services.Configure<EasSyncOptions>(
    builder.Configuration.GetSection(EasSyncOptions.SectionName));

var rateLimitPermit = builder.Configuration.GetValue<int?>("Eas:RateLimit:PermitPerWindow") ?? 100;
var rateLimitWindowSeconds = builder.Configuration.GetValue<int?>("Eas:RateLimit:WindowSeconds") ?? 10;
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/health") || path.StartsWithSegments("/alive"))
            return RateLimitPartition.GetNoLimiter("health");

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rateLimitPermit,
            Window = TimeSpan.FromSeconds(rateLimitWindowSeconds),
            QueueLimit = 0,
        });
    });
});

builder.Services.AddSingleton<OrphanFolderTracker>();
builder.Services.AddSingleton<PingRegistry>();
builder.Services.AddSingleton<ISyncRequestCache, RedisSyncRequestCache>();
builder.Services.AddSingleton<MailboxChangeNotifier>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MailboxChangeNotifier>());

builder.Services.AddScoped<FolderSyncService>();
builder.Services.AddScoped<MeProfileWriteback>();
builder.Services.AddScoped<ItemSyncService>();
builder.Services.AddScoped<GetItemEstimateService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<OutboundMailService>();
builder.Services.AddScoped<AttachmentService>();
builder.Services.AddScoped<PushMonitor>();
builder.Services.AddScoped<PingService>();

var app = builder.Build();

if (app.Services.GetRequiredService<IOptions<EasSyncOptions>>().Value.AbsentSupportedClearsOmitted)
    app.Logger.LogWarning(
        "{Key} is ON: clients that primed without a Supported element will have omitted " +
        "contact and calendar elements deleted on every Change",
        $"{EasSyncOptions.SectionName}:{nameof(EasSyncOptions.AbsentSupportedClearsOmitted)}");

app.UseForwardedHeaders();
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseAuthentication();

app.UseMiddleware<ActiveSyncMiddleware>();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
