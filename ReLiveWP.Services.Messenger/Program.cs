using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Messenger.Data;
using ReLiveWP.Services.Messenger.Endpoints;
using ReLiveWP.Services.Messenger.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddRedis(builder.Configuration);
builder.Services.AddScoped<MsnpGatewaySessionStore>();
builder.Services.AddScoped<MsnpGatewayService>();

builder.Services.AddPolledGauge("relivewp.messenger.sessions.open", TimeSpan.FromSeconds(30),
    (sp, ct) => MsnpGatewaySessionStore.CountSessionsAsync(sp.GetRequiredService<IConnectionMultiplexer>(), ct),
    unit: "{session}");

builder.Services.AddGrpcClient<Authentication.AuthenticationClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));

var app = builder.Build();

app.MapMsnpGateway();
app.MapDefaultEndpoints();

app.Run();
