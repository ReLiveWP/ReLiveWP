using System.Text.Json;
using ReLiveWP.ServiceDefaults.Events;
using StackExchange.Redis;

namespace ReLiveWP.Backend.Mailbox.Services;

// keeps the EAS Me contact in step with the account profile, off the account.profile-changed broadcast.
public class AccountProfileChangedSubscriber(
    IConnectionMultiplexer redis,
    IServiceScopeFactory scopeFactory,
    ILogger<AccountProfileChangedSubscriber> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await redis.GetSubscriber().SubscribeAsync(AccountEvents.ProfileChanged, (channel, value) => { _ = HandleAsync(value); });
        logger.LogInformation("listening for account.profile-changed");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await redis.GetSubscriber().UnsubscribeAsync(AccountEvents.ProfileChanged);
    }

    private async Task HandleAsync(RedisValue value)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<AccountProfileChangedEvent>((byte[])value!);
            if (evt is null)
                return;

            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<MeContactMirrorService>().MirrorAsync(evt);
            await scope.ServiceProvider.GetRequiredService<ContactLinkResolver>().ReconcileForOwnerAsync(evt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed mirroring from account.profile-changed");
        }
    }
}
