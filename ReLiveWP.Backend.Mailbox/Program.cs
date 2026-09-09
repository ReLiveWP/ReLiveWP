using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Services;
using ReLiveWP.Backend.Mailbox.Services.Grpc;
using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Grpc;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddGrpc(options =>
{
    const int maxMessageSize = 32 * 1024 * 1024;
    options.MaxReceiveMessageSize = maxMessageSize;
    options.MaxSendMessageSize = maxMessageSize;
});
builder.Services.AddRedis(builder.Configuration);

builder.Services.AddSingleton<ChangeLogInterceptor>();
builder.Services.AddSingleton<ItemValidationInterceptor>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MailboxDbContext>((sp, options) =>
    options.UseNpgsql(connectionString)
           // validation must run first so a rejected batch never reaches the change log
           .AddInterceptors(
               sp.GetRequiredService<ItemValidationInterceptor>(),
               sp.GetRequiredService<ChangeLogInterceptor>()));
builder.Services.AddReadinessCheck("postgres", (sp, ct) => sp.GetRequiredService<MailboxDbContext>().Database.CanConnectAsync(ct));

builder.Services.AddGrpcClient<User.UserClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));

builder.Services.AddScoped<MailboxProvisioningService>();
builder.Services.AddScoped<MailboxIntegrityService>();
builder.Services.AddScoped<SyncStateRepairService>();
builder.Services.AddScoped<MailboxDeletionService>();
builder.Services.AddScoped<MailboxRetentionSweepService>();
builder.Services.AddScoped<MeContactMirrorService>();
builder.Services.AddScoped<ContactLinkResolver>();
builder.Services.AddHostedService<AccountCreatedSubscriber>();
builder.Services.AddHostedService<AccountDeletedSubscriber>();
builder.Services.AddHostedService<AccountProfileChangedSubscriber>();
builder.Services.AddHostedService<MailboxBackfillService>();
builder.Services.AddHostedService<ContactLinkBackfillService>();
builder.Services.AddHostedService<MailboxRepairService>();
builder.Services.AddHostedService<SyncStateRepairBackgroundService>();
builder.Services.AddHostedService<MailboxRetentionService>();

var gaugeInterval = TimeSpan.FromMinutes(5);
builder.Services.AddPolledGauge("relivewp.mailbox.changelog.rows", gaugeInterval,
    (sp, ct) => sp.GetRequiredService<MailboxDbContext>().ItemEvents.LongCountAsync(ct), "{row}");
builder.Services.AddPolledGauge("relivewp.mailbox.items.soft_deleted", gaugeInterval,
    (sp, ct) => sp.GetRequiredService<MailboxDbContext>().Items.LongCountAsync(i => i.DeletedAt != null, ct), "{item}");
builder.Services.AddPolledGauge("relivewp.mailbox.syncstates.rows", gaugeInterval,
    (sp, ct) => sp.GetRequiredService<MailboxDbContext>().SyncStates.LongCountAsync(ct), "{row}");
builder.Services.AddPolledGauge("relivewp.mailbox.devices.known", gaugeInterval,
    (sp, ct) => sp.GetRequiredService<MailboxDbContext>().DeviceInfos.LongCountAsync(ct), "{device}");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<MailboxDbContext>().Database.Migrate();

app.MapGrpcService<MailboxStoreService>();
app.MapGrpcService<EmailService>();

app.MapDefaultEndpoints();

app.Run();
