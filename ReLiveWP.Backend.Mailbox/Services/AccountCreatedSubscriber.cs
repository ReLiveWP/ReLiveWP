using System.Text.Json;
using ReLiveWP.ServiceDefaults.Events;
using StackExchange.Redis;

namespace ReLiveWP.Backend.Mailbox.Services;

// provisions a mailbox as soon as an account is created, off the account.created broadcast.
public class AccountCreatedSubscriber(
    IConnectionMultiplexer redis,
    IServiceScopeFactory scopeFactory,
    ILogger<AccountCreatedSubscriber> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await redis.GetSubscriber().SubscribeAsync(AccountEvents.Created, (channel, value) => { _ = HandleAsync(value); });
        logger.LogInformation("listening for account.created");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await redis.GetSubscriber().UnsubscribeAsync(AccountEvents.Created);
    }

    private async Task HandleAsync(RedisValue value)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<AccountCreatedEvent>((byte[])value!);
            if (evt is null)
                return;

            using var scope = scopeFactory.CreateScope();
            var provisioner = scope.ServiceProvider.GetRequiredService<MailboxProvisioningService>();
            await provisioner.ProvisionAsync(evt.UserId, evt.Email, evt.Username);

            // the seed only knows the username, so pull the real name and tile over
            var mirror = scope.ServiceProvider.GetRequiredService<MeContactMirrorService>();
            await mirror.MirrorFromIdentityAsync(evt.UserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed provisioning from account.created");
        }
    }
}
