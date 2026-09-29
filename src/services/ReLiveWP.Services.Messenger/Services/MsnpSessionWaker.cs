using ReLiveWP.Services.Messenger.Data;

namespace ReLiveWP.Services.Messenger.Services;

public class MsnpSessionWaker(
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime lifetime,
    ILogger<MsnpSessionWaker> logger) : IMsnpSessionWaker
{
    public void WakeSessionLater(string sessionId) => _ = WakeSessionAsync(sessionId, lifetime.ApplicationStopping);

    private async Task WakeSessionAsync(string sessionId, CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var sessions = scope.ServiceProvider.GetRequiredService<IMsnpGatewaySessionStore>();
            if (!await sessions.HasPendingAsync(sessionId))
                return;

            await scope.ServiceProvider.GetRequiredService<IMsnpDoorbell>().RingIfIdleAsync(sessionId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "could not wake session {SessionId}", sessionId);
        }
    }
}
