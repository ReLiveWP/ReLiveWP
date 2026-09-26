using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ConnectedServices.Data;

namespace ReLiveWP.Backend.ConnectedServices.Services;

public class TokenRefreshService(ILogger<TokenRefreshService> logger,
                                 IServiceScopeFactory scopeFactory,
                                 IConnectedServicesContainer connectedServices,
                                 ServiceTokenLocks tokenLocks) : BackgroundService
{
    private static readonly TimeSpan FirstRunDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(FirstRunDelay);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            timer.Period = Interval;
            logger.LogInformation("Token refresh started...");

            try
            {
                await RefreshDueTokensAsync(stoppingToken);
                logger.LogInformation("Token refresh completed!");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Something went very wrong when refreshing tokens!!");
            }
        }
    }

    private async Task RefreshDueTokensAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConnectedServicesDbContext>();

        foreach (var service in await dbContext.ConnectedServices.ToListAsync(ct))
        {
            if (!connectedServices.TryGetValue(service.Service, out var serviceDescription))
            {
                logger.LogWarning("Found unavailable service {ServiceType} in {ServiceId}", service.Service, service.Id);
                dbContext.ConnectedServices.Remove(service);
                continue;
            }

            if (!service.IsDueForRefresh)
                continue;

            await using var serviceLock = await tokenLocks.AcquireAsync(service.Id, ct);
            if (!serviceLock.IsAcquired)
            {
                logger.LogWarning("Timeout acquiring lock for {ServiceId}, potential deadlock?", service.Id);
                continue;
            }

            await dbContext.Entry(service).ReloadAsync(ct);
            if (dbContext.Entry(service).State == EntityState.Detached || !service.IsDueForRefresh)
                continue;

            // credential-linked services have nothing to refresh, so a rejection means the
            // stored password no longer works and the user has to relink
            if (serviceDescription.OAuthHandler == null)
            {
                logger.LogWarning("Credentials for {ServiceId} were rejected, marking for relink", service.Id);
                service.ApplyRefreshResult(refreshed: false);
                await dbContext.SaveChangesOverConcurrentWritesAsync(ct);
                continue;
            }

            var handler = await serviceDescription.OAuthHandler(scope.ServiceProvider);
            var refreshed = await handler.RefreshTokensAsync(service);

            if (refreshed)
                logger.LogInformation("Successfully refreshed tokens for {ServiceId}!", service.Id);
            else
                logger.LogError("Failed to refresh tokens for {ServiceId}!", service.Id);

            service.ApplyRefreshResult(refreshed);
            await dbContext.SaveChangesOverConcurrentWritesAsync(ct);
        }
    }
}
