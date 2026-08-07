using System.Text.Json;
using ReLiveWP.ServiceDefaults.Events;
using StackExchange.Redis;

namespace ReLiveWP.Backend.Mailbox.Services;

// hard-deletes a mailbox as soon as an account is deleted, off the account.deleted broadcast.
public class AccountDeletedSubscriber(
    IConnectionMultiplexer redis,
    IServiceScopeFactory scopeFactory,
    ILogger<AccountDeletedSubscriber> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await redis.GetSubscriber().SubscribeAsync(AccountEvents.Deleted, (channel, value) => { _ = HandleAsync(value); });
        logger.LogInformation("listening for account.deleted");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await redis.GetSubscriber().UnsubscribeAsync(AccountEvents.Deleted);
    }

    private async Task HandleAsync(RedisValue value)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<AccountDeletedEvent>((byte[])value!);
            if (evt is null)
                return;

            using var scope = scopeFactory.CreateScope();
            var deletion = scope.ServiceProvider.GetRequiredService<MailboxDeletionService>();
            await deletion.DeleteAsync(evt.UserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed deleting mailbox from account.deleted");
        }
    }
}
