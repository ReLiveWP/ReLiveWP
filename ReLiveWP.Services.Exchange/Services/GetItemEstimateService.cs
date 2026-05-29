using Microsoft.EntityFrameworkCore;
using ReLiveWP.Services.Exchange.Data;
using ReLiveWP.Services.Exchange.Data.Entities;
using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Services;

// Backs the GetItemEstimate command. Returns the number of items that would be
// sent in the next Sync for each requested collection, using the same watermark
// and SyncEngine.Collapse logic as ItemSyncService.
public class GetItemEstimateService
{
    private readonly ExchangeDbContext _db;

    public GetItemEstimateService(ExchangeDbContext db) => _db = db;

    public async Task<GetItemEstimateResponse> EstimateAsync(
        string userId, string deviceId, GetItemEstimateRequest request, CancellationToken ct = default)
    {
        var response = new GetItemEstimateResponse();

        foreach (var coll in request.Collections?.Items ?? [])
            response.Responses.Add(await EstimateCollectionAsync(userId, deviceId, coll, ct));

        return response;
    }

    private async Task<GieResponse> EstimateCollectionAsync(
        string userId, string deviceId, GieRequestCollection req, CancellationToken ct)
    {
        var collectionId = req.CollectionId;

        var state = await _db.SyncStates.SingleOrDefaultAsync(
            s => s.UserId == userId && s.DeviceId == deviceId && s.CollectionId == collectionId, ct);

        if (state is null)
            return MakeError(collectionId, 3); // state not primed — client must Sync(SyncKey=0) first

        if (state.SyncKey != req.SyncKey)
            return MakeError(collectionId, 4); // invalid SyncKey

        int estimate;

        if (state.Watermark == -1)
        {
            // First real sync pending — all live items will come down as Adds
            estimate = await _db.Items
                .CountAsync(i => i.UserId == userId && i.CollectionId == collectionId && i.DeletedAt == null, ct);
        }
        else
        {
            var events = await _db.ItemEvents
                .Where(e => e.UserId == userId && e.CollectionId == collectionId && e.Id > state.Watermark)
                .ToListAsync(ct);

            var delta = SyncEngine.Collapse(events);
            estimate = delta.Added.Count + delta.Updated.Count + delta.Deleted.Count;
        }

        var folder = await _db.Folders.FirstOrDefaultAsync(
            f => f.UserId == userId && f.Id == collectionId, ct);

        var itemClass = folder?.Type switch
        {
            FolderType.CalendarDefault or FolderType.Calendar => "Calendar",
            FolderType.ContactsDefault or FolderType.Contacts => "Contacts",
            FolderType.TasksDefault or FolderType.Task         => "Tasks",
            _ => "Email",
        };

        return new GieResponse
        {
            Status = 1,
            Collection = new GieResponseCollection
            {
                CollectionId = collectionId,
                Class = itemClass,
                Estimate = estimate,
            },
        };
    }

    private static GieResponse MakeError(string collectionId, int status) => new()
    {
        Status = status,
        Collection = new GieResponseCollection { CollectionId = collectionId },
    };
}
