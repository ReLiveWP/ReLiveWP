using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Tests;

// a contact added from the device is linked while its Add is being handled, which happens after the
// response's change set has already been built. These pin down when the device actually sees it.
public class LinkedContactDeliveryTests
{
    private const string User = "u1";
    private const string Device = "d1";
    private const string Collection = "contacts";
    private const string ServerId = "contact-1";
    private const string Cid = "15fe5d7a6d8d65ff";

    private static ItemSyncService NewService(FakeMailboxStoreClient client) =>
        new(client, NullLogger<ItemSyncService>.Instance, Options.Create(new EasSyncOptions()));

    [Fact]
    public async Task The_add_response_carries_no_annotation_and_leaves_the_event_above_the_watermark()
    {
        UpsertSyncStateRequest? written = null;
        var client = new FakeMailboxStoreClient
        {
            OnGetSyncState = _ => Primed(watermark: 5),
            OnGetItemEvents = _ => [],
            OnGetFolder = _ => new Folder { Id = Collection, Type = ReLiveWP.Services.Grpc.Mailbox.FolderType.ContactsDefault },
            OnCreateItem = _ => Linked(),
            OnUpsertSyncState = req =>
            {
                written = req;
                return new SyncState { UserId = User, DeviceId = Device, CollectionId = Collection };
            },
        };

        var response = await NewService(client).SyncAsync(User, Device, AddRequest());

        Assert.NotNull(response.Responses);
        Assert.Equal(ServerId, response.Responses!.Add[0].ServerId);

        // the link is real on the server at this point, but nothing in this response tells the device
        Assert.Null(response.Commands);

        // the add's own event lands above this, so the next sync will pick it up rather than skip it
        Assert.NotNull(written);
        Assert.Equal(5, written!.Watermark);
    }

    [Fact]
    public async Task The_next_sync_delivers_the_annotation()
    {
        var client = new FakeMailboxStoreClient
        {
            OnGetSyncState = _ => Primed(watermark: 5),
            OnGetItemEvents = _ => [new ItemEvent { Id = 6, CommitId = 6, ServerId = ServerId, EventType = ChangeEventType.Add }],
            OnGetItems = _ => [Linked()],
            OnUpsertSyncState = _ => new SyncState { UserId = User, DeviceId = Device, CollectionId = Collection },
        };

        var response = await NewService(client).SyncAsync(User, Device, new SyncCollection
        {
            CollectionId = Collection,
            SyncKey = "1",
            GetChanges = true,
        });

        Assert.NotNull(response.Commands);
        var data = response.Commands!.Add.Single(a => a.ServerId == ServerId).ApplicationData;

        var annotations = data.Elements.Single(e => e.LocalName == "Annotations");
        var names = annotations.ChildNodes.Cast<System.Xml.XmlNode>()
            .Select(n => n.SelectSingleNode("*[local-name()='Name']")!.InnerText)
            .ToList();

        Assert.Contains("CID", names);
        Assert.Contains("IMMRI", names);
    }

    private static SyncCollection AddRequest()
    {
        var doc = new System.Xml.XmlDocument();
        var email = doc.CreateElement("Email1Address", Constants.Contacts);
        email.InnerText = "amy@relivewp.net";

        return new SyncCollection
        {
            CollectionId = Collection,
            SyncKey = "1",
            GetChanges = true,
            Commands = new SyncCommands
            {
                Add =
                {
                    new SyncAdd
                    {
                        ClientId = "c1",
                        ApplicationData = new ApplicationData { Elements = { email } },
                    }
                },
            },
        };
    }

    private static SyncState Primed(long watermark) => new()
    {
        UserId = User,
        DeviceId = Device,
        CollectionId = Collection,
        SyncKey = "1",
        Watermark = watermark,
        CachedAnnotationNames = "CID,OID,WLID,IMMRI,Type,UserTileUrl",
        PreviousSyncKey = "0",
        PreviousWatermark = 0,
    };

    private static Item Linked() => new()
    {
        ServerId = ServerId,
        CollectionId = Collection,
        Contact = new ContactItem
        {
            FirstName = "amy",
            Email1Address = "amy@relivewp.net",
            Annotation = new ContactAnnotation
            {
                Cid = unchecked((long)0x15fe5d7a6d8d65ff),
                WlId = "amy@relivewp.net",
                ImMri = "1:amy@relivewp.net",
            },
        },
    };
}
