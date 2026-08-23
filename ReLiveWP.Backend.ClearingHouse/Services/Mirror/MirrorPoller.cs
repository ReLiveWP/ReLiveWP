using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using RedLockNet.SERedis;
using ReLiveWP.Backend.ClearingHouse.Data;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror;

public class MirrorPoller(
    MirrorKind kind,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    RedLockFactory locks,
    ILogger<MirrorPoller> logger) : BackgroundService
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(5);

    private const int MaxConsecutiveFailures = 6;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(Tick);

        do
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                logger.LogError(e, "{Kind} mirror sweep failed; will retry next interval", kind);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClearingHouseDbContext>();

        var interval = configuration.GetValue($"Mirror:{kind}:Interval", DefaultInterval);
        var due = DateTime.UtcNow - interval;

        var sources = await db.SyncSources
            .Where(s => s.Kind == kind
                    && (s.RunRequestedAt != null
                     || (s.SyncEnabled
                         && s.ConsecutiveFailures < MaxConsecutiveFailures
                         && (s.LastAttemptedAt == null || s.LastAttemptedAt < due))))
            .OrderByDescending(s => s.RunRequestedAt)
            .ThenBy(s => s.LastAttemptedAt)
            .Select(s => s.Id)
            .ToListAsync(ct);

        foreach (var id in sources)
        {
            if (ct.IsCancellationRequested) return;
            await RunOneAsync(id, due, ct);
        }
    }

    private async Task RunOneAsync(string sourceId, DateTime due, CancellationToken ct)
    {
        await using var handle = await locks.CreateLockAsync(
            $"clearinghouse:mirror:{sourceId}", TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(200), ct);

        if (!handle.IsAcquired) return;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClearingHouseDbContext>();
        var connected = scope.ServiceProvider.GetRequiredService<ConnectedServices.ConnectedServicesClient>();

        var detach = scope.ServiceProvider.GetRequiredService<SyncSourceDetach>();

        var runner = scope.ServiceProvider.GetServices<IMirrorRunner>().FirstOrDefault(r => r.Kind == kind)
            ?? throw new MirrorException($"No mirror runner registered for {kind}");

        var source = await db.SyncSources.FirstOrDefaultAsync(s => s.Id == sourceId, ct);
        if (source is null) return;

        var pull = source.RunRequestedAt is not null
            || (source.SyncEnabled
                && source.ConsecutiveFailures < MaxConsecutiveFailures
                && (source.LastAttemptedAt is null || source.LastAttemptedAt < due));

        source.RunStartedAt = DateTime.UtcNow;
        source.LastAttemptedAt = source.RunStartedAt;
        source.RunRequestedAt = null;
        await db.SaveChangesAsync(ct);

        try
        {
            var resolved = await ResolveConnectionAsync(connected, source, ct);

            if (resolved is null)
            {
                logger.LogInformation("connection {Connection} is gone; detaching {Service}/{Source} and keeping its items",
                    source.ConnectionId, source.ServiceId, source.SourceId);

                await detach.DetachAsync([source], ct);
                return;
            }

            // turning the capability off on the connection stops the pull, whatever the source row says
            if (!resolved.Supplies(kind))
            {
                source.SyncEnabled = false;
                return;
            }

            if (resolved.Usable is not { } connection)
            {
                logger.LogInformation("connection {Connection} is unusable, leaving {Service} {Kind} in place",
                    source.ConnectionId, source.ServiceId, kind);

                source.LastFailure = "the connection needs relinking";
                return;
            }

            if (!pull) return;

            var result = await runner.RunAsync(source, connection, ct);

            source.LastRunCreated = result.Created;
            source.LastRunUpdated = result.Updated;
            source.LastRunDeleted = result.Deleted;
            source.LastRunSkipped = result.Skipped;

            if (!result.DidNothing)
                logger.LogInformation("imported {Service}/{Source} for {User}: +{Created} ~{Updated} -{Deleted} ={Skipped}",
                    source.ServiceId, source.SourceId, source.UserId,
                    result.Created, result.Updated, result.Deleted, result.Skipped);

            if (resolved.IsTransient)
            {
                await detach.DetachAsync([source], ct);
                await DiscardConnectionAsync(connected, source, ct);
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            source.ConsecutiveFailures++;
            source.LastFailureAt = DateTime.UtcNow;
            source.LastFailure = e.Message;

            logger.LogWarning(e, "mirror run {Attempt} failed for {Service}/{Source}",
                source.ConsecutiveFailures, source.ServiceId, source.SourceId);
        }
        finally
        {
            source.RunStartedAt = null;
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task DiscardConnectionAsync(
        ConnectedServices.ConnectedServicesClient connected, DbSyncSource source, CancellationToken ct)
    {
        try
        {
            var headers = new Metadata { { "X-User-Id", source.UserId } };

            await connected.DeleteConnectionAsync(
                new DeleteConnectionRequest { ConnectionId = source.ConnectionId },
                headers, cancellationToken: ct);

            logger.LogInformation("discarded the transient connection {Connection} after importing {Service}/{Source}",
                source.ConnectionId, source.ServiceId, source.SourceId);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // the sweeper in ConnectedServices expires it anyway; the items are already safe
            logger.LogWarning(e, "could not discard the transient connection {Connection}", source.ConnectionId);
        }
    }

    // an import runs on a connection nothing else is allowed to see
    private static Task<ResolvedConnection?> ResolveConnectionAsync(
        ConnectedServices.ConnectedServicesClient connected, DbSyncSource source, CancellationToken ct) =>
        ConnectionLookup.ResolveAsync(connected, source.UserId, source.ConnectionId,
            headers: new Metadata { { "X-User-Id", source.UserId } }, includeTransient: true, ct);
}
