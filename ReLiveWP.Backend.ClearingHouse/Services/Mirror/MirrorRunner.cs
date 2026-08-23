using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ClearingHouse.Data;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror;

public interface IMirrorRunner
{
    MirrorKind Kind { get; }

    Task<MirrorRunResult> RunAsync(DbSyncSource source, SyncConnection connection, CancellationToken ct = default);
}

public abstract class MirrorRunner(
    MailboxStore.MailboxStoreClient mailbox,
    ClearingHouseDbContext db,
    MirrorDriverRegistry drivers,
    ILogger logger) : IMirrorRunner
{
    public abstract MirrorKind Kind { get; }

    // where a write lands. contacts merge into the one default folder, calendar gets one per source.
    protected abstract ValueTask<string> ResolveFolderAsync(
        DbSyncSource source, SyncConnection connection, CancellationToken ct);

    // last chance to touch the items before they are written, e.g. pulling contact photos
    protected virtual Task PrepareAsync(
        IReadOnlyList<MirrorWrite> writes, SyncConnection connection, CancellationToken ct) =>
        Task.CompletedTask;

    public async Task<MirrorRunResult> RunAsync(
        DbSyncSource source, SyncConnection connection, CancellationToken ct = default)
    {
        if (!drivers.TryGet(Kind, source.ServiceId, out var driver))
            throw new MirrorException($"No {Kind} driver for '{source.ServiceId}'");

        MirrorBatch batch;
        var expired = false;
        try
        {
            batch = await driver.FetchChangesAsync(connection, source.SourceId, source.DeltaToken, ct);
        }
        catch (DeltaTokenExpiredException)
        {
            logger.LogInformation("delta token expired for {Service}/{Source}, pulling in full",
                source.ServiceId, source.SourceId);

            expired = true;
            batch = await driver.FetchChangesAsync(connection, source.SourceId, null, ct);
        }

        var result = await ApplyAsync(source, connection, batch, ct);

        // an expired token gets dropped even if the full pull came back without a new one, else we
        // write the dead one back and do this again next poll
        if (batch.DeltaToken is not null || expired)
            source.DeltaToken = batch.DeltaToken;

        source.LastSyncedAt = DateTime.UtcNow;
        source.ConsecutiveFailures = 0;
        source.LastFailure = null;
        await db.SaveChangesAsync(ct);

        return result;
    }

    private async Task<MirrorRunResult> ApplyAsync(
        DbSyncSource source, SyncConnection connection, MirrorBatch batch, CancellationToken ct)
    {
        var userId = connection.UserId;
        var known = await ReadKnownAsync(userId, source, ct);

        var writes = MirrorPlanner.PlanWrites(known, batch);
        var gone = MirrorPlanner.PlanDeletes(known, batch);

        // every item in the batch either gets written or gets left alone
        var skipped = batch.Items.Count - writes.Count;
        int created = 0, updated = 0;

        await PrepareAsync(writes, connection, ct);

        var folderId = writes.Any(w => w.ServerId is null)
            ? await ResolveFolderAsync(source, connection, ct)
            : null;

        foreach (var (remote, serverId) in writes)
        {
            var origin = BuildOrigin(source, remote);

            try
            {
                if (serverId is not null)
                {
                    var request = new UpdateItemRequest
                    {
                        UserId = userId,
                        ServerId = serverId,
                        Origin = origin,
                    };

                    remote.ApplyTo(request);
                    await mailbox.UpdateItemAsync(request, cancellationToken: ct);

                    updated++;
                }
                else
                {
                    var request = new CreateItemRequest
                    {
                        UserId = userId,
                        CollectionId = folderId,
                        Origin = origin,
                    };

                    remote.ApplyTo(request);
                    await mailbox.CreateItemAsync(request, cancellationToken: ct);

                    created++;
                }
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.InvalidArgument)
            {
                logger.LogWarning("skipping {Service} item {External}: {Message}",
                    source.ServiceId, remote.ExternalId, e.Status.Detail);
                skipped++;
            }
        }

        if (batch.Unreadable.Count > 0)
            logger.LogWarning("{Count} unreadable item(s) on {Service}/{Source}; leaving them alone",
                batch.Unreadable.Count, source.ServiceId, source.SourceId);

        var deleted = await DeleteAsync(userId, source, gone, ct);

        return new MirrorRunResult(created, updated, deleted, skipped);
    }

    private async Task<int> DeleteAsync(
        string userId, DbSyncSource source, List<string> gone, CancellationToken ct)
    {
        if (gone.Count == 0)
            return 0;

        var result = await mailbox.DeleteItemsByOriginAsync(new DeleteItemsByOriginRequest
        {
            UserId = userId,
            OriginServiceId = source.ServiceId,
            OriginCollectionId = source.SourceId,
            ExternalIds = { gone },
        }, cancellationToken: ct);

        return result.ItemsDeleted;
    }

    private static ItemOrigin BuildOrigin(DbSyncSource source, IRemoteItem remote)
    {
        var origin = new ItemOrigin
        {
            ServiceId = source.ServiceId,
            CollectionId = source.SourceId,
            ExternalId = remote.ExternalId,
            SyncedAt = Timestamp.FromDateTime(DateTime.UtcNow),
        };

        if (remote.Etag is not null) origin.Etag = remote.Etag;

        return origin;
    }

    private async Task<Dictionary<string, KnownItem>> ReadKnownAsync(
        string userId, DbSyncSource source, CancellationToken ct)
    {
        var map = new Dictionary<string, KnownItem>(StringComparer.Ordinal);

        using var call = mailbox.ListItemsByOrigin(new ListItemsByOriginRequest
        {
            UserId = userId,
            OriginServiceId = source.ServiceId,
            OriginCollectionId = source.SourceId,
            IncludeDeleted = true,
        }, cancellationToken: ct);

        await foreach (var item in call.ResponseStream.ReadAllAsync(ct))
        {
            if (string.IsNullOrEmpty(item.ExternalId)) continue;

            var known = new KnownItem(
                item.ServerId, item.HasEtag ? item.Etag : null, item.Deleted, item.RemoteSynced);

            // a live row always wins over a soft-deleted one for the same remote object
            if (!map.TryGetValue(item.ExternalId, out var current) || (current.IsDeleted && !known.IsDeleted))
                map[item.ExternalId] = known;
        }

        return map;
    }
}
