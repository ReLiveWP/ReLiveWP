using Microsoft.Extensions.Options;

namespace ReLiveWP.Backend.Chat.Services;

public class PresenceSweeper(
    IServiceScopeFactory scopeFactory,
    IOptions<ChatOptions> options,
    ILogger<PresenceSweeper> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.SweepInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<PresenceService>().SweepAsync(stoppingToken);
                await scope.ServiceProvider.GetRequiredService<MessageService>().DropExpiredStoredAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "presence sweep failed, trying again next tick");
            }
        }
    }
}
