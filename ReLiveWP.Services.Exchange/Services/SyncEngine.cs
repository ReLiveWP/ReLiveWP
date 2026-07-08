using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Services;

public record SyncDelta(List<string> Added, List<string> Updated, List<string> Deleted, long Watermark);
public record struct SyncEvent(long Id, string ServerId, ChangeEventType EventType);

public static class SyncEngine
{
    public static SyncDelta Collapse(IReadOnlyList<SyncEvent> events)
    {
        var added   = new List<string>();
        var updated = new List<string>();
        var deleted = new List<string>();
        long watermark = 0;

        foreach (var group in events.GroupBy(e => e.ServerId))
        {
            var ordered = group.OrderBy(e => e.Id).ToList();
            var first = ordered[0].EventType;
            var last  = ordered[^1].EventType;

            if (first == ChangeEventType.Add && last == ChangeEventType.Delete)
                continue;

            switch (last)
            {
                case ChangeEventType.Delete: deleted.Add(group.Key); break;
                case ChangeEventType.Add:    added.Add(group.Key);   break;
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
