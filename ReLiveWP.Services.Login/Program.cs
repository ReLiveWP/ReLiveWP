using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.RateLimiting;
using Grpc.Net.ClientFactory;
using Microsoft.AspNetCore.RateLimiting;
using ReLiveWP.Identity;
using ReLiveWP.Identity.Grpc;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.DeviceRegistration;
using ReLiveWP.Services.Login;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddLiveIDAuthentication((o) =>
{
    o.ConnectedServicesGrpcConfiguration = (c) => c.Address = new Uri(builder.Configuration["Endpoints:ConnectedServices:Grpc"]!);
    o.LiveIDConfiguration = (c) => c.ValidServiceTargets = ["http://Passport.NET/tb", "relivewp.net"];
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AuthForwardingInterceptor>();

builder.Services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));

builder.Services.AddRazorTemplating();

builder.Services.AddGrpcClient<User.UserClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));
builder.Services.AddGrpcClient<Authentication.AuthenticationClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));

builder.Services.AddGrpcClient<ConnectedServices.ConnectedServicesClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:ConnectedServices:Grpc"]!))
    .AddInterceptor<AuthForwardingInterceptor>(InterceptorScope.Client);

builder.Services.AddGrpcClient<ClientProvisioning.ClientProvisioningClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:ClientProvisioning"]!));
builder.Services.AddGrpcClient<DeviceRegistration.DeviceRegistrationClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:DeviceRegistration"]!));

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("DeviceRegisterLimit", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromSeconds(10);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
});

var app = builder.Build();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();
app.Run();
