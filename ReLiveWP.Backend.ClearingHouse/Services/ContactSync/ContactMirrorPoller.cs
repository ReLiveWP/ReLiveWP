using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using RedLockNet.SERedis;
using ReLiveWP.Backend.ClearingHouse.Data;
using ReLiveWP.ServiceDefaults.Contacts;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public class ContactMirrorPoller(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    RedLockFactory locks,
    ILogger<ContactMirrorPoller> logger) : BackgroundService
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
                logger.LogError(e, "contact mirror sweep failed; will retry next interval");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClearingHouseDbContext>();

        var interval = configuration.GetValue("Mirror:Contacts:Interval", DefaultInterval);
        var due = DateTime.UtcNow - interval;

        var sources = await db.ContactSyncSources
            .Where(s => s.RunRequestedAt != null
                     || (!s.DetachAfterRun
                         && s.ConsecutiveFailures < MaxConsecutiveFailures
                         && (s.LastSyncedAt == null || s.LastSyncedAt < due)))
            .OrderByDescending(s => s.RunRequestedAt)
            .ThenBy(s => s.LastSyncedAt)
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
        var mirror = scope.ServiceProvider.GetRequiredService<ContactMirrorService>();
        var connected = scope.ServiceProvider.GetRequiredService<ConnectedServices.ConnectedServicesClient>();

        var detach = scope.ServiceProvider.GetRequiredService<ContactSourceDetach>();

        var source = await db.ContactSyncSources.FirstOrDefaultAsync(s => s.Id == sourceId, ct);
        if (source is null) return;

        var requested = source.RunRequestedAt is not null;

        var pull = requested
            || (!source.DetachAfterRun
                && source.ConsecutiveFailures < MaxConsecutiveFailures
                && (source.LastSyncedAt is null || source.LastSyncedAt < due));

        source.RunStartedAt = DateTime.UtcNow;
        source.RunRequestedAt = null;
        await db.SaveChangesAsync(ct);

        try
        {
            var resolved = await ResolveConnectionAsync(connected, source, ct);

            if (resolved is null)
            {
                logger.LogInformation("connection {Connection} is gone; detaching {Service}/{Source} and keeping its contacts",
                    source.ConnectionId, source.ServiceId, source.SourceId);

                await detach.DetachAsync([source], ct);
                return;
            }

            if (resolved.Usable is not { } connection)
            {
                logger.LogInformation("connection {Connection} is unusable, leaving {Service} contacts in place",
                    source.ConnectionId, source.ServiceId);

                source.LastFailure = "the connection needs relinking";
                return;
            }

            if (!pull) return;

            var result = await mirror.RunAsync(source, connection, requested, ct);

            source.LastRunCreated = result.Created;
            source.LastRunUpdated = result.Updated;
            source.LastRunDeleted = result.Deleted;
            source.LastRunSkipped = result.Skipped;

            if (!result.DidNothing)
                logger.LogInformation("imported {Service}/{Source} for {User}: +{Created} ~{Updated} -{Deleted} ={Skipped}",
                    source.ServiceId, source.SourceId, source.UserId,
                    result.Created, result.Updated, result.Deleted, result.Skipped);

            if (source.DetachAfterRun)
            {
                await detach.DetachAsync([source], ct);

                if (resolved.IsTransient)
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

    private const ulong BustedFlag = ConnectionConsts.BustedFlag;
    private const ulong TransientFlag = ConnectionConsts.TransientFlag;

    private async Task DiscardConnectionAsync(
        ConnectedServices.ConnectedServicesClient connected, DbContactSyncSource source, CancellationToken ct)
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
            // the sweeper in ConnectedServices expires it anyway; the contacts are already safe
            logger.LogWarning(e, "could not discard the transient connection {Connection}", source.ConnectionId);
        }
    }

    // an import runs on a connection nothing else is allowed to see
    private static Task<ResolvedConnection?> ResolveConnectionAsync(
        ConnectedServices.ConnectedServicesClient connected, DbContactSyncSource source, CancellationToken ct) =>
        ConnectionLookup.ResolveAsync(connected, source.UserId, source.ConnectionId,
            headers: new Metadata { { "X-User-Id", source.UserId } }, includeTransient: true, ct);
}
