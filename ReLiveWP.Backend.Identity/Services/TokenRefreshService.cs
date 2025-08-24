using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Identity.ConnectedServices;
using ReLiveWP.Backend.Identity.Data;

namespace ReLiveWP.Backend.Identity.Services;

public class TokenRefreshService(ILogger<TokenRefreshService> logger,
                                 IServiceProvider services,
                                 IConnectedServicesContainer connectedServices,
                                 ServiceTokenLocks tokenLocks) : IHostedService, IDisposable
{
    private PeriodicTimer? refreshTimer;
    private Task? timerTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        refreshTimer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        timerTask = Task.Run(TimerLoop, cancellationToken);

        logger.LogInformation("Token refresh service started");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        refreshTimer?.Dispose();

        logger.LogInformation("Token refresh service stopped");

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        refreshTimer?.Dispose();
    }

    private async Task TimerLoop()
    {
        while (await refreshTimer!.WaitForNextTickAsync())
        {
            refreshTimer.Period = TimeSpan.FromHours(1);

            logger.LogInformation("Token refresh started...");

            try
            {
                using var scope = services.CreateScope();
                using var dbContext = scope.ServiceProvider.GetRequiredService<LiveDbContext>();

                foreach (var tmpService in (await dbContext.ConnectedServices.ToListAsync()))
                {
                    if (!connectedServices.TryGetValue(tmpService.Service, out var serviceDescription))
                    {
                        // unsupported service!
                        logger.LogWarning("Found unavailable service {ServiceType} in {ServiceId}", tmpService.Service, tmpService.Id);
                        dbContext.ConnectedServices.Remove(tmpService);
                        continue;
                    }

                    var serviceLock = tokenLocks.GetOrCreateLock(tmpService.Id);
                    if (!await serviceLock.WaitAsync(5000))
                    {
                        logger.LogWarning("Timeout aquiring lock for {ServiceId}, potential deadlock?", tmpService.Id);
                        continue;
                    }

                    try
                    {
                        var service = await dbContext.ConnectedServices.FindAsync(tmpService.Id);
                        if (service == null)
                            continue;

                        if (service.ExpiresAt <= DateTime.UtcNow ||
                            (service.Flags & LiveConnectedServiceFlags.NeedsRefresh) == LiveConnectedServiceFlags.NeedsRefresh)
                        {
                            var handler = await serviceDescription.OAuthHandler(scope.ServiceProvider);
                            if (!await handler.RefreshTokensAsync(service))
                            {
                                logger.LogError("Failed to refresh tokens for {ServiceId}!", tmpService.Id);
                                service.Flags = LiveConnectedServiceFlags.Busted;
                            }
                            else
                            {
                                logger.LogInformation("Successfully refreshed tokens for {ServiceId}!", tmpService.Id);
                                service.Flags = LiveConnectedServiceFlags.None;
                            }

                            dbContext.ConnectedServices.Update(service);
                            await dbContext.SaveChangesAsync(); // inefficient but we need to maintain consistency
                        }
                    }
                    finally
                    {
                        serviceLock.Release();
                    }
                }

                var expiredOauths = (await dbContext.PendingOAuths
                    .ToListAsync())
                    .Where(a => a.ExpiresAt < DateTimeOffset.UtcNow);

                foreach (var expiredOauth in expiredOauths)
                    dbContext.PendingOAuths.Remove(expiredOauth);

                await dbContext.SaveChangesAsync();

                logger.LogInformation("Token refresh completed!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Something went very wrong when refreshing tokens!!");
            }
        }
    }
}
