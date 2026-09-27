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
    
public record struct SyncEvent(long CommitId, long Id, string ServerId, ChangeEventType EventType);

public static class SyncEngine
{
    public const int DefaultWindowSize = 100;
    public const int MaxWindowSize = 512;

    public static int ResolveWindowSize(int? requested) => requested switch
    {
        null => DefaultWindowSize,
        < 0 => DefaultWindowSize,
        0 => MaxWindowSize,
        > MaxWindowSize => MaxWindowSize,
        _ => requested.Value,
    };

    public static SyncDelta Collapse(IReadOnlyList<SyncEvent> events, int? windowSize = null)
    {
        var groups = new List<(string ServerId, ChangeEventType First, ChangeEventType Last, long MinCommit, long MaxCommit)>();

        foreach (var group in events.GroupBy(e => e.ServerId))
        {
            var ordered = group.OrderBy(e => e.CommitId).ThenBy(e => e.Id).ToList();
            var first = ordered[0].EventType;
            var last  = ordered[^1].EventType;
            var minCommit = ordered[0].CommitId;
            var maxCommit = ordered[^1].CommitId;

            if (first == ChangeEventType.Add && last == ChangeEventType.Delete)
                continue;

            groups.Add((group.Key, first, last, minCommit, maxCommit));
        }

        // full unwindowed Updated set, computed before any windowing cut
        var allUpdated = groups
            .Where(g => g.Last == ChangeEventType.Update && g.First != ChangeEventType.Add)
            .Select(g => g.ServerId)
            .ToHashSet(StringComparer.Ordinal);

        // Ordered by earliest event, not latest. The watermark after a truncated window is
        // "just before the first event we did not deliver", so an excluded group must never own
        // an earlier event than an included one - otherwise the watermark moves backwards, the
        // next request re-reads the same events, and the collection never drains.
        groups.Sort((a, b) => a.MinCommit != b.MinCommit
            ? a.MinCommit.CompareTo(b.MinCommit)
            : a.MaxCommit.CompareTo(b.MaxCommit));

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
            // truncated
            long lastIncludedMaxCommit = windowed[^1].MaxCommit;
            long minExcludedCommit = groups.Skip(windowSize!.Value).Min(g => g.MinCommit);
            watermark = Math.Min(lastIncludedMaxCommit, minExcludedCommit - 1);

            // The sort above guarantees this, but the cost of being wrong is an unbreakable
            // client loop rather than a wrong answer, so it is worth asserting.
            long firstIncludedMinCommit = windowed[0].MinCommit;
            if (watermark < firstIncludedMinCommit) watermark = firstIncludedMinCommit;
        }
        else
        {
            watermark = 0;
            foreach (var e in events)
                if (e.CommitId > watermark) watermark = e.CommitId;
        }

        return new SyncDelta(added, updated, deleted, watermark, moreAvailable, allUpdated);
    }

    public static string NextSyncKey(string current) =>
        long.TryParse(current, out var n) ? (n + 1).ToString() : "1";


    public static void SuppressDeleted(SyncCommands? commands, IEnumerable<string> deletedServerIds)
    {
        if (commands is null) return;
        var deleted = deletedServerIds as IReadOnlySet<string> ?? deletedServerIds.ToHashSet(StringComparer.Ordinal);
        if (deleted.Count == 0) return;
        commands.Add.RemoveAll(a => a.ServerId is not null && deleted.Contains(a.ServerId));
        commands.Change.RemoveAll(c => deleted.Contains(c.ServerId));
    }
}
