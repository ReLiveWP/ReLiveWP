using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Services;

public record SyncDelta(List<string> Added, List<string> Updated, List<string> Deleted, long Watermark);

// Minimal event projection passed to Collapse — decoupled from both the EF entity
// and the specific proto message type (FolderEvent vs ItemEvent share the same fields).
public record SyncEvent(long Id, string ServerId, ChangeEventType EventType);

public static class SyncEngine
{
    // Collapses a window of change-log events into the net Add/Update/Delete set a
    // device needs, plus the new watermark. An item Added then Deleted in the same
    // window is silently dropped — the device never saw it.
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
