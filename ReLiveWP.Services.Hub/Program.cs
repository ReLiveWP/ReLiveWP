using System.Text.Json;
using Grpc.Net.ClientFactory;
using ReLiveWP.Identity;
using ReLiveWP.Identity.Grpc;
using ReLiveWP.Services.Grpc.ClearingHouse;
using ReLiveWP.Services.Hub;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddControllers(options => options.Filters.Add<GrpcFaultFilter>())
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

builder.Services.AddGrpcClient<ClearingHouse.ClearingHouseClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:ClearingHouse"]!))
    .AddInterceptor<AuthForwardingInterceptor>(InterceptorScope.Client);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
