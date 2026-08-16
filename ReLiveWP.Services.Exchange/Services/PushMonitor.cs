using Grpc.Core;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Services;

public class PushMonitor(
    MailboxStore.MailboxStoreClient mailbox,
    MailboxChangeNotifier notifier)
{
    public async Task<List<string>> GetChangedCollectionsAsync(string userId,
                                                               string deviceId,
                                                               IEnumerable<string> collectionIds,
                                                               CancellationToken ct = default)
    {
        var changed = new List<string>();

        foreach (var collectionId in collectionIds)
        {
            SyncState? state;
            try
            {
                state = await mailbox.GetSyncStateAsync(
                    new GetSyncStateRequest { UserId = userId, DeviceId = deviceId, CollectionId = collectionId },
                    cancellationToken: ct);
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                if (await TipAsync(userId, collectionId, ct) > 0)
                    changed.Add(collectionId);

                continue;
            }

            if (state.Watermark == -1)
            {
                changed.Add(collectionId);
                continue;
            }

            if (await TipAsync(userId, collectionId, ct) > state.Watermark)
                changed.Add(collectionId);
        }

        return changed;
    }

    private async Task<long> TipAsync(string userId, string collectionId, CancellationToken ct) =>
        (await mailbox.GetItemEventTipAsync(
            new ItemEventTipRequest { UserId = userId, CollectionId = collectionId },
            cancellationToken: ct)).Value;

    public async Task<List<string>> WaitForChangesAsync(string userId,
                                                        string deviceId,
                                                        IReadOnlySet<string> collectionIds,
                                                        DateTimeOffset deadline,
                                                        CancellationToken requestAborted)
    {
        var changed = await GetChangedCollectionsAsync(userId, deviceId, collectionIds, requestAborted);

        while (changed.Count == 0 && DateTimeOffset.UtcNow < deadline && !requestAborted.IsCancellationRequested)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
            cts.CancelAfter(deadline - DateTimeOffset.UtcNow);

            await notifier.WaitForChangeAsync(userId, collectionIds, cts.Token);

            if (requestAborted.IsCancellationRequested)
                break;

            changed = await GetChangedCollectionsAsync(userId, deviceId, collectionIds, requestAborted);
        }

        return changed;
    }
}
