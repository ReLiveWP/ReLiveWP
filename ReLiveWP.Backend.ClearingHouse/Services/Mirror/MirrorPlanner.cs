namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror;

public readonly record struct MirrorWrite(IRemoteItem Remote, string? ServerId);

public sealed record KnownItem(string ServerId, string? Etag, bool IsDeleted, bool RemoteSynced);

public static class MirrorPlanner
{
    public static List<MirrorWrite> PlanWrites(IReadOnlyDictionary<string, KnownItem> known, MirrorBatch batch)
    {
        var writes = new List<MirrorWrite>(batch.Items.Count);
        var seen = new HashSet<string>(batch.Items.Count, StringComparer.Ordinal);

        foreach (var remote in batch.Items)
        {
            if (!seen.Add(remote.ExternalId)) continue;

            known.TryGetValue(remote.ExternalId, out var existing);

            // once anything but us has written the item it is the user's, and no pull touches it again
            if (existing is { IsDeleted: false, RemoteSynced: false }) continue;

            // a deleted row keeps the etag it had, so testing it here would make asking for a fresh
            // copy do nothing at all
            if (existing is { IsDeleted: false } && existing.Etag == remote.Etag && remote.Etag is not null)
                continue;

            // deleting a synced item is how you ask for a fresh copy, so a soft-deleted row gets a
            // new one beside it rather than being updated in place
            writes.Add(new(remote, existing is { IsDeleted: false } ? existing.ServerId : null));
        }

        return writes;
    }

    public static List<string> PlanDeletes(IReadOnlyDictionary<string, KnownItem> known, MirrorBatch batch)
    {
        var gone = new List<string>();

        if (!batch.IsFullSync)
        {
            foreach (var id in batch.DeletedExternalIds)
                if (known.TryGetValue(id, out var k) && k.RemoteSynced)
                    gone.Add(id);

            return gone;
        }

        var mentioned = new HashSet<string>(batch.Items.Count, StringComparer.Ordinal);
        foreach (var remote in batch.Items)
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
}
