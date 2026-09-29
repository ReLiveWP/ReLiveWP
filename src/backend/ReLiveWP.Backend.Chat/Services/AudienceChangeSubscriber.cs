using Microsoft.Extensions.Options;
using ReLiveWP.ServiceDefaults.Events;
using StackExchange.Redis;

namespace ReLiveWP.Backend.Chat.Services;

// an address book edit or a visibility change can move who sees whom. mailbox.changed fires for
// every item, mail included, so each user gets at most one refresh per delay window across instances
public class AudienceChangeSubscriber(
    IConnectionMultiplexer redis,
    IServiceScopeFactory scopeFactory,
    IOptions<ChatOptions> options,
    ILogger<AudienceChangeSubscriber> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mailboxChanges = redis.ReadEventsAsync<MailboxChangedEvent>(
            MailboxEvents.Changed, evt => _ = ScheduleRefreshAsync(evt.UserId, stoppingToken), logger, stoppingToken);
        var profileChanges = redis.ReadEventsAsync<AccountProfileChangedEvent>(
            AccountEvents.ProfileChanged, evt => _ = ScheduleRefreshAsync(evt.UserId, stoppingToken), logger, stoppingToken);

        return Task.WhenAll(mailboxChanges, profileChanges);
    }

    private async Task ScheduleRefreshAsync(string userId, CancellationToken ct)
    {
        try
        {
            var delay = options.Value.AudienceRefreshDelay;
            var claimed = await redis.GetDatabase()
                .StringSetAsync($"chat:audience-refresh:{userId}", 1, delay, When.NotExists);
            if (!claimed)
                return;

            await Task.Delay(delay, ct);

            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<PresenceService>().RefreshAudienceAsync(userId, ct);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "audience refresh for {User} failed", userId);
        }
    }
}
