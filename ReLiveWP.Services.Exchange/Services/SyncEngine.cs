using ReLiveWP.Services.Exchange.Data.Entities;

namespace ReLiveWP.Services.Exchange.Services;

public record SyncDelta(List<string> Added, List<string> Updated, List<string> Deleted, long Watermark);

// Shared sync utilities used by both FolderSyncService and ItemSyncService.
public static class SyncEngine
{
    // Collapses an unordered window of change-log events into the net set of
    // Add/Update/Delete ServerIds a device needs, plus the new watermark.
    // An item Added then Deleted within the same window is silently dropped —
    // the device never saw it, so there is nothing to do.
    public static SyncDelta Collapse(IReadOnlyList<IChangeEvent> events)
    {
        var added = new List<string>();
        var updated = new List<string>();
        var deleted = new List<string>();
        long watermark = 0;

        foreach (var group in events.GroupBy(e => e.ServerId))
        {
            var ordered = group.OrderBy(e => e.Id).ToList();
            var first = ordered[0].EventType;
            var last = ordered[^1].EventType;

            if (first == ChangeEventType.Add && last == ChangeEventType.Delete)
                continue;

            switch (last)
            {
                case ChangeEventType.Delete: deleted.Add(group.Key); break;
                case ChangeEventType.Add: added.Add(group.Key); break;
                case ChangeEventType.Update:
                    (first == ChangeEventType.Add ? added : updated).Add(group.Key);
                    break;
            }
        }

        foreach (var e in events)
            if (e.Id > watermark) watermark = e.Id;

        return new SyncDelta(added, updated, deleted, watermark);
    }

    public static string NextSyncKey(string current) =>
        long.TryParse(current, out var n) ? (n + 1).ToString() : "1";
}
