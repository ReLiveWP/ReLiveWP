using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using ReLiveWP.Backend.Chat;
using ReLiveWP.Backend.Chat.Data;
using ReLiveWP.Backend.Chat.Grpc;
using ReLiveWP.Backend.Chat.Services;
using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddGrpc();
builder.Services.Configure<ChatOptions>(builder.Configuration.GetSection(ChatOptions.SectionName));

builder.Services.AddRedis(builder.Configuration);
builder.Services.AddSingleton<IDistributedLockFactory>(sp =>
    RedLockFactory.Create([new RedLockMultiplexer(sp.GetRequiredService<IConnectionMultiplexer>())]));

builder.Services.AddGrpcClient<MailboxStore.MailboxStoreClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Mailbox"]!));
builder.Services.AddGrpcClient<User.UserClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:User"]!));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IChatStateStore, RedisChatStateStore>();
builder.Services.AddSingleton<IDeliveryQueue, RedisDeliveryQueue>();
builder.Services.AddSingleton<IStoredMessageStore, RedisStoredMessageStore>();
builder.Services.AddScoped<IAudienceSource, MailboxAudienceSource>();
builder.Services.AddScoped<IUserDirectory, IdentityUserDirectory>();
builder.Services.AddScoped<PresenceService>();
builder.Services.AddScoped<MessageService>();

builder.Services.AddHostedService<PresenceSweeper>();
builder.Services.AddHostedService<AudienceChangeSubscriber>();

builder.Services.AddPolledGauge("relivewp.chat.endpoints.registered", TimeSpan.FromMinutes(1),
    (sp, ct) => sp.GetRequiredService<IChatStateStore>().CountEndpointsAsync(), "{endpoint}");

var app = builder.Build();

app.MapGrpcService<ChatService>();
app.MapDefaultEndpoints();

app.Run();
