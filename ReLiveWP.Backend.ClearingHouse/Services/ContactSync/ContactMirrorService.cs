using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ClearingHouse.Data;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public sealed record MirrorRunResult(int Created, int Updated, int Deleted, int Skipped)
{
    public bool DidNothing => Created == 0 && Updated == 0 && Deleted == 0;
}

public class ContactMirrorService(
    MailboxStore.MailboxStoreClient mailbox,
    ClearingHouseDbContext db,
    ContactSyncDriverRegistry drivers,
    ContactPhotoService photos,
    ContactsFolderResolver folders,
    IConfiguration configuration,
    ILogger<ContactMirrorService> logger)
{
    private const int DefaultPhotoConcurrency = 8;

    public async Task<MirrorRunResult> RunAsync(
        DbContactSyncSource source, SyncConnection connection, bool force = false, CancellationToken ct = default)
    {
        if (!drivers.TryGet(source.ServiceId, out var driver))
            throw new ContactSyncException($"No contact driver for '{source.ServiceId}'");

        ContactSyncBatch batch;
        try
        {
            batch = await driver.FetchChangesAsync(connection, source.SourceId, source.DeltaToken, ct);
        }
        catch (DeltaTokenExpiredException)
        {
            logger.LogInformation("delta token expired for {Service}/{Source}, pulling in full",
                source.ServiceId, source.SourceId);

            batch = await driver.FetchChangesAsync(connection, source.SourceId, null, ct);
        }

        var result = await ApplyAsync(source, connection, driver, batch, force, ct);

        source.DeltaToken = batch.DeltaToken ?? source.DeltaToken;
        source.LastSyncedAt = DateTime.UtcNow;
        source.ConsecutiveFailures = 0;
        source.LastFailure = null;
        await db.SaveChangesAsync(ct);

        return result;
    }

    private async Task<MirrorRunResult> ApplyAsync(
        DbContactSyncSource source, SyncConnection connection, IContactSyncDriver driver,
        ContactSyncBatch batch, bool force, CancellationToken ct)
    {
        var userId = connection.UserId;
        var known = await ReadKnownAsync(userId, source, ct);

        var writes = PlanWrites(known, batch, force);
        var gone = PlanDeletes(known, batch);

        // every contact in the batch either gets written or gets left alone
        var skipped = batch.Contacts.Count - writes.Count;
        int created = 0, updated = 0;

        await FetchPhotosAsync(writes, connection, ct);

        var folderId = writes.Any(w => w.ServerId is null)
            ? await folders.ResolveAsync(userId, ct)
            : null;

        foreach (var (remote, serverId) in writes)
        {
            var origin = BuildOrigin(source, remote);

            try
            {
                if (serverId is not null)
                {
                    await mailbox.UpdateItemAsync(new UpdateItemRequest
                    {
                        UserId = userId,
                        ServerId = serverId,
                        Contact = remote.Contact,
                        Origin = origin,
                    }, cancellationToken: ct);

                    updated++;
                }
                else
                {
                    await mailbox.CreateItemAsync(new CreateItemRequest
                    {
                        UserId = userId,
                        CollectionId = folderId,
                        Contact = remote.Contact,
                        Origin = origin,
                    }, cancellationToken: ct);

                    created++;
                }
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.InvalidArgument)
            {
                logger.LogWarning("skipping {Service} contact {External}: {Message}",
                    source.ServiceId, remote.ExternalId, e.Status.Detail);
                skipped++;
            }
        }

        if (batch.Unreadable.Count > 0)
            logger.LogWarning("{Count} unreadable card(s) on {Service}/{Source}; leaving them alone",
                batch.Unreadable.Count, source.ServiceId, source.SourceId);

        var deleted = await DeleteAsync(userId, source, gone, ct);

        return new MirrorRunResult(created, updated, deleted, skipped);
    }

    internal static List<MirrorWrite> PlanWrites(IReadOnlyDictionary<string, KnownItem> known, ContactSyncBatch batch, bool force)
    {
        var writes = new List<MirrorWrite>(batch.Contacts.Count);
        var seen = new HashSet<string>(batch.Contacts.Count, StringComparer.Ordinal);

        foreach (var remote in batch.Contacts)
        {
            if (!seen.Add(remote.ExternalId)) continue;

            known.TryGetValue(remote.ExternalId, out var existing);

            // once anything but us has written the item it is the user's, and no pull touches it again
            if (existing is { IsDeleted: false, RemoteSynced: false }) continue;

            // a deleted row keeps the etag it had, so testing it here would make asking for a fresh
            // copy do nothing at all
            if (!force && existing is { IsDeleted: false } && existing.Etag == remote.Etag && remote.Etag is not null)
                continue;

            // deleting a synced contact is how you ask for a fresh copy, so a soft-deleted row gets a
            // new one beside it rather than being updated in place
            writes.Add(new(remote, existing is { IsDeleted: false } ? existing.ServerId : null));
        }

        return writes;
    }

    internal static List<string> PlanDeletes(IReadOnlyDictionary<string, KnownItem> known, ContactSyncBatch batch)
    {
        var gone = new List<string>();

        if (!batch.IsFullSync)
        {
            foreach (var id in batch.DeletedExternalIds)
                if (known.TryGetValue(id, out var k) && k.RemoteSynced)
                    gone.Add(id);

            return gone;
        }

        var mentioned = new HashSet<string>(batch.Contacts.Count, StringComparer.Ordinal);
        foreach (var remote in batch.Contacts)
            mentioned.Add(remote.ExternalId);
        foreach (var id in batch.Unreadable)
            mentioned.Add(id);

        foreach (var (id, item) in known)
        {
            if (item is { IsDeleted: false, RemoteSynced: true } && !mentioned.Contains(id))
                gone.Add(id);
        }

        return gone;
    }

    private Task FetchPhotosAsync(IReadOnlyList<MirrorWrite> pending, SyncConnection connection, CancellationToken ct)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = configuration.GetValue("Mirror:Contacts:PhotoConcurrency", DefaultPhotoConcurrency),
            CancellationToken = ct,
        };

        return Parallel.ForEachAsync(pending, options, async (entry, token) =>
        {
            if (await photos.ResolveAsync(entry.Remote, connection, token) is { } picture)
                entry.Remote.Contact.Picture = ByteString.CopyFrom(picture);
        });
    }

    private async Task<int> DeleteAsync(
        string userId, DbContactSyncSource source, List<string> gone, CancellationToken ct)
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

    private static ItemOrigin BuildOrigin(DbContactSyncSource source, RemoteContact remote)
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

    // a contact the pull will write, and the row it lands on. no row means create.
    internal readonly record struct MirrorWrite(RemoteContact Remote, string? ServerId);

    internal sealed record KnownItem(string ServerId, string? Etag, bool IsDeleted, bool RemoteSynced);

    private async Task<Dictionary<string, KnownItem>> ReadKnownAsync(string userId, DbContactSyncSource source, CancellationToken ct)
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
