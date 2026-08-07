using Grpc.Core;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc.Mailbox;
using ProtoFolderType = ReLiveWP.Services.Grpc.Mailbox.FolderType;

namespace ReLiveWP.Services.Exchange.Services;

public class GetItemEstimateService(MailboxStore.MailboxStoreClient mailbox, OrphanFolderTracker orphans)
{
    public const int StatusSuccess = 1;
    public const int StatusInvalidCollection = 2;
    public const int StatusNotPrimed = 3;
    public const int StatusInvalidSyncKey = 4;

    public async Task<GetItemEstimateResponse> EstimateAsync(string userId,
                                                             string deviceId,
                                                             GetItemEstimateRequest request,
                                                             CancellationToken ct = default)
    {
        var response = new GetItemEstimateResponse();

        foreach (var coll in request.Collections?.Items ?? [])
        {
            var r = await EstimateCollectionAsync(userId, deviceId, coll, ct);
            if (r is not null) response.Responses.Add(r);
        }

        return response;
    }

    private async Task<GieResponse?> EstimateCollectionAsync(string userId,
                                                             string deviceId,
                                                             GieRequestCollection req,
                                                             CancellationToken ct)
    {
        var collectionId = req.CollectionId;

        // a stale/unknown collection is status 2 (invalid collection) and recorded for the next
        // FolderSync delete. GetItemEstimate only defines 1-4; 8 is a Sync status and isn't valid here.
        Folder folder;
        try
        {
            folder = await mailbox.GetFolderAsync(
                new GetFolderRequest { UserId = userId, ServerId = collectionId },
                cancellationToken: ct);
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            orphans.Record(deviceId, collectionId);
            return MakeError(collectionId, StatusInvalidCollection);
        }

        SyncState? state;
        try
        {
            state = await mailbox.GetSyncStateAsync(
                new GetSyncStateRequest { UserId = userId, DeviceId = deviceId, CollectionId = collectionId },
                cancellationToken: ct);
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            return MakeError(collectionId, StatusNotPrimed);
        }

        var itemClass = folder.Type switch
        {
            ProtoFolderType.CalendarDefault or ProtoFolderType.Calendar => "Calendar",
            ProtoFolderType.ContactsDefault or ProtoFolderType.Contacts or ProtoFolderType.MeContact => "Contacts",
            ProtoFolderType.TasksDefault or ProtoFolderType.Task => "Tasks",
            _ => "Email",
        };

        // a one-deep retransmit recomputes from the checkpoint watermark (mirrors Sync); anything older is Status 4
        long estimateWatermark;
        if (state.SyncKey == req.SyncKey)
        {
            estimateWatermark = state.Watermark;
        }
        else if (req.SyncKey == state.PreviousSyncKey)
        {
            estimateWatermark = state.PreviousWatermark;
        }
        else
        {
            return MakeError(collectionId, StatusInvalidSyncKey);
        }

        int estimate;

        if (estimateWatermark == -1)
        {
            estimate = (await mailbox.CountLiveItemsAsync(
                new CountLiveItemsRequest { UserId = userId, CollectionId = collectionId },
                cancellationToken: ct)).Count;
        }
        else
        {
            var events = await ReadAllItemEventsAsync(userId, collectionId, estimateWatermark, ct);
            var delta = SyncEngine.Collapse(events.Select(e => new SyncEvent(e.CommitId, e.Id, e.ServerId, e.EventType)).ToList());
            estimate = delta.Added.Count + delta.Updated.Count + delta.Deleted.Count;
        }

        return new GieResponse
        {
            Status = StatusSuccess,
            Collection = new GieResponseCollection
            {
                CollectionId = collectionId,
                Class = itemClass,
                Estimate = estimate,
            },
        };
    }

    private async Task<List<ItemEvent>> ReadAllItemEventsAsync(string userId,
                                                               string collectionId,
                                                               long afterWatermark,
                                                               CancellationToken ct)
    {
        var result = new List<ItemEvent>();
        using var call = mailbox.GetItemEvents(new GetItemEventsRequest
        {
            UserId = userId,
            CollectionId = collectionId,
            AfterWatermark = afterWatermark
        }, cancellationToken: ct);

        await foreach (var e in call.ResponseStream.ReadAllAsync(ct))
            result.Add(e);
        return result;
    }

    private static GieResponse MakeError(string collectionId, int status) => new()
    {
        Status = status,
        Collection = new GieResponseCollection { CollectionId = collectionId },
    };
}
