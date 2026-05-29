using Microsoft.EntityFrameworkCore;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Data;
using ReLiveWP.Services.Exchange.Middleware;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc;

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

builder.Services.AddControllers();
builder.Services.AddSingleton<EasRequestLog>();
builder.Services.AddDbContext<ExchangeDbContext>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<FolderSyncService>();
builder.Services.AddScoped<ItemSyncService>();
builder.Services.AddScoped<GetItemEstimateService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<ProvisioningService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<ExchangeDbContext>().Database.Migrate();

app.UseHttpsRedirection();
app.UseAuthentication();

// Must come before UseRouting() so EasCommandAttribute can read the parsed command
// from HttpContext.Items during endpoint selection (auto-inserted UseRouting() runs
// before user middleware, causing the constraint to see an empty Items dictionary).
app.UseMiddleware<ActiveSyncMiddleware>();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
