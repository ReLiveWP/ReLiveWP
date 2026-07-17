using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Services;

public record SyncDelta(
    List<string> Added,
    List<string> Updated,
    List<string> Deleted,
    long Watermark,
    bool MoreAvailable,
    IReadOnlySet<string> AllUpdatedServerIds);
public record struct SyncEvent(long Id, string ServerId, ChangeEventType EventType);

public static class SyncEngine
{
    public const int DefaultWindowSize = 100;
    public const int MaxWindowSize = 512;

    // WindowSize (MS-ASCMD 2.2.3.199): absent or <= 0 -> 100 default, > 512 -> clamp to 512, else pass-through
    public static int ResolveWindowSize(int? requested) => requested switch
    {
        null => DefaultWindowSize,
        <= 0 => DefaultWindowSize,
        > MaxWindowSize => MaxWindowSize,
        _ => requested.Value,
    };

    public static SyncDelta Collapse(IReadOnlyList<SyncEvent> events, int? windowSize = null)
    {
        var groups = new List<(string ServerId, ChangeEventType First, ChangeEventType Last, long MinId, long MaxId)>();

        foreach (var group in events.GroupBy(e => e.ServerId))
        {
            var ordered = group.OrderBy(e => e.Id).ToList();
            var first = ordered[0].EventType;
            var last  = ordered[^1].EventType;
            var minId = ordered[0].Id;
            var maxId = ordered[^1].Id;

            if (first == ChangeEventType.Add && last == ChangeEventType.Delete)
                continue;

            groups.Add((group.Key, first, last, minId, maxId));
        }

        // full unwindowed Updated set, computed before any windowing cut
        var allUpdated = groups
            .Where(g => g.Last == ChangeEventType.Update && g.First != ChangeEventType.Add)
            .Select(g => g.ServerId)
            .ToHashSet(StringComparer.Ordinal);

        groups.Sort((a, b) => a.MaxId.CompareTo(b.MaxId));

        bool moreAvailable = windowSize is { } w && groups.Count > w;
        var windowed = moreAvailable ? groups.Take(windowSize!.Value).ToList() : groups;

        var added   = new List<string>();
        var updated = new List<string>();
        var deleted = new List<string>();

        foreach (var g in windowed)
        {
            switch (g.Last)
            {
                case ChangeEventType.Delete: deleted.Add(g.ServerId); break;
                case ChangeEventType.Add:    added.Add(g.ServerId);   break;
                case ChangeEventType.Update:
                    (g.First == ChangeEventType.Add ? added : updated).Add(g.ServerId);
                    break;
            }
        }

        long watermark;
        if (moreAvailable)
        {
            // truncated: normally advance to the last included group's MaxId, so the remainder
            // is re-fetched (not skipped) on the follow-up sync with the new SyncKey. But event
            // ids interleave across items (one item's Add can be older than another item's later
            // Update), so an excluded group's earliest event can still sit below that cut point.
            // If we advanced there anyway, the next sync would read that group AfterWatermark and
            // see only its tail event(s) - misclassifying a never-delivered item as a Change. Clamp
            // to just below the smallest event id among the excluded groups so any straddling group
            // is left whole for next time (and re-read there in full, as an Add).
            long lastIncludedMaxId = windowed[^1].MaxId;
            long minExcludedId = groups.Skip(windowSize!.Value).Min(g => g.MinId);
            watermark = Math.Min(lastIncludedMaxId, minExcludedId - 1);
        }
        else
        {
            // unwindowed (or everything fit): advance past every event seen this fetch,
            // including no-op groups (e.g. Add+Delete within the same window), or the next
            // sync re-reads them forever
            watermark = 0;
            foreach (var e in events)
                if (e.Id > watermark) watermark = e.Id;
        }

        return new SyncDelta(added, updated, deleted, watermark, moreAvailable, allUpdated);
    }

    public static string NextSyncKey(string current) =>
        long.TryParse(current, out var n) ? (n + 1).ToString() : "1";

    // the client's delete wins: re-sending an item it just asked to remove wedges strict clients (WP7)
    public static void SuppressDeleted(SyncCommands? commands, IEnumerable<string> deletedServerIds)
    {
        if (commands is null) return;
        var deleted = deletedServerIds as IReadOnlySet<string> ?? deletedServerIds.ToHashSet(StringComparer.Ordinal);
        if (deleted.Count == 0) return;
        commands.Add.RemoveAll(a => a.ServerId is not null && deleted.Contains(a.ServerId));
        commands.Change.RemoveAll(c => deleted.Contains(c.ServerId));
    }
}
