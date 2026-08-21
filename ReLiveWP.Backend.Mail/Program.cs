using ReLiveWP.Backend.Mail;
using ReLiveWP.Backend.Mail.Grpc;
using ReLiveWP.Backend.Mail.Services;
using ReLiveWP.Identity;
using ReLiveWP.Identity.Grpc;
using ReLiveWP.Mail;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddGrpc(o => o.MaxReceiveMessageSize = 32 * 1024 * 1024);
builder.Services.AddGrpcAuthentication();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AuthForwardingInterceptor>();

builder.Services.Configure<MailOptions>(builder.Configuration.GetSection(MailOptions.SectionName));

builder.Services.AddRedis(builder.Configuration);

builder.Services.AddGrpcClient<MailboxStore.MailboxStoreClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Mailbox"]!))
    .ConfigureChannel(ch => ch.MaxSendMessageSize = 32 * 1024 * 1024);

builder.Services.AddGrpcClient<User.UserClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:User"]!));

builder.Services.AddSingleton<MimeIngest>();
builder.Services.AddSingleton<IMailQueue, RedisMailQueue>();
builder.Services.AddScoped<IRecipientRouter, RecipientRouter>();
builder.Services.AddScoped<IMailDeliveryAgent, LocalDeliveryAgent>();
builder.Services.AddScoped<ISentItemsWriter, SentItemsWriter>();

builder.Services.AddHostedService<MailDeliveryWorker>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<MailSubmissionService>();
app.MapDefaultEndpoints();

app.Run();
