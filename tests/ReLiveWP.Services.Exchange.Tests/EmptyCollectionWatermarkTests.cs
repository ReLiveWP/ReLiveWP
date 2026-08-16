using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Tests;

public class EmptyCollectionWatermarkTests
{
    private const string User = "u1";
    private const string Device = "d1";
    private const string Collection = "empty-folder";

    private static ItemSyncService NewService(FakeMailboxStoreClient client) =>
        new(client, NullLogger<ItemSyncService>.Instance,
            Options.Create(new EasSyncOptions()));

    private static SyncState Primed() => new()
    {
        UserId = User,
        DeviceId = Device,
        CollectionId = Collection,
        SyncKey = "1",
        Watermark = -1,
        PreviousSyncKey = "0",
        PreviousWatermark = 0,
    };

    [Fact]
    public async Task An_empty_collection_leaves_the_never_synced_marker_behind()
    {
        UpsertSyncStateRequest? written = null;
        var client = new FakeMailboxStoreClient
        {
            OnGetSyncState = _ => Primed(),
            OnGetItemEvents = _ => [],
            OnUpsertSyncState = req =>
            {
                written = req;
                return new SyncState { UserId = User, DeviceId = Device, CollectionId = Collection };
            },
        };

        await NewService(client).SyncAsync(User, Device, new SyncCollection
        {
            CollectionId = Collection,
            SyncKey = "1",
            GetChanges = true,
        });

        Assert.NotNull(written);
        Assert.NotEqual(-1, written!.Watermark);
    }

    [Fact]
    public async Task A_no_content_sync_of_an_empty_collection_does_the_same()
    {
        UpsertSyncStateRequest? written = null;
        var client = new FakeMailboxStoreClient
        {
            OnGetSyncState = _ => Primed(),
            OnGetItemEvents = _ => [],
            OnUpsertSyncState = req =>
            {
                written = req;
                return new SyncState { UserId = User, DeviceId = Device, CollectionId = Collection };
            },
        };

        await NewService(client).SyncAsync(User, Device, new SyncCollection
        {
            CollectionId = Collection,
            SyncKey = "1",
            GetChanges = false,
        });

        Assert.NotNull(written);
        Assert.NotEqual(-1, written!.Watermark);
    }
}
