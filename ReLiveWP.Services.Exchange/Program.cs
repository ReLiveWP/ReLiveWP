using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Middleware;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddLiveIDAuthentication(opts =>
{
    opts.IdentityGrpcConfiguration = o =>
        o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!);
    opts.LiveIDConfiguration = o =>
    {
        o.AcceptBasicAuth = true;
        o.ValidServiceTargets = ["http://Passport.NET/tb"];
    };
});

builder.Services.AddGrpcClient<User.UserClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));

builder.Services.AddGrpcClient<MailboxStore.MailboxStoreClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Mailbox"]!));

builder.Services.AddControllers();

builder.Services.AddSingleton<EasRequestLog>();

builder.Services.AddScoped<FolderSyncService>();
builder.Services.AddScoped<ItemSyncService>();
builder.Services.AddScoped<GetItemEstimateService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<ProvisioningService>();
builder.Services.AddScoped<OutboundMailService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();

app.UseMiddleware<ActiveSyncMiddleware>();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
