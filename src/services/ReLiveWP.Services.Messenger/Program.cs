using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Chat;
using ReLiveWP.Services.Messenger;
using ReLiveWP.Services.Messenger.Data;
using ReLiveWP.Services.Messenger.Endpoints;
using ReLiveWP.Services.Messenger.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.Configure<MessengerOptions>(builder.Configuration.GetSection(MessengerOptions.SectionName));
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IMsnpGatewaySessionStore, MsnpGatewaySessionStore>();
builder.Services.AddScoped<IMsnpDeviceStore, MsnpDeviceStore>();
builder.Services.AddScoped<MsnpGatewayService>();
builder.Services.AddScoped<IMsnpDoorbell, MsnpDoorbell>();
builder.Services.AddSingleton<IMsnpSessionWaker, MsnpSessionWaker>();
builder.Services.AddHttpClient(MsnpDoorbell.HttpClientName);
builder.Services.AddHostedService<ChatDeliverySubscriber>();
builder.Services.AddHostedService<StoredMessageSubscriber>();

builder.Services.AddPolledGauge("relivewp.messenger.sessions.open", TimeSpan.FromSeconds(30),
    (sp, ct) => MsnpGatewaySessionStore.CountSessionsAsync(sp.GetRequiredService<IConnectionMultiplexer>(), ct),
    unit: "{session}");

builder.Services.AddGrpcClient<Authentication.AuthenticationClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));
builder.Services.AddGrpcClient<Chat.ChatClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Chat"]!));

var app = builder.Build();

app.MapMsnpGateway();
app.MapDefaultEndpoints();

app.Run();
