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
                SourceId = "WL",
                ShellContactType = "Regular",
                MobileImEnabled = true,
            },
        },
    };

    // the real 8.1 annotation set, captured off the wire: no IMMRI, MRI/OtherMRI instead
    private const string EightOneNames =
        "CID,WLID,Type,ShellContactType,SID,OID,MRI,OtherMRI,MobileIMEnabled,UserTileUrl,UserTileHash";

    private static Item Mirrored(bool annotated) => new()
    {
        ServerId = ServerId,
        CollectionId = Collection,
        Origin = new ItemOrigin { ServiceId = "google", CollectionId = "people/me/connections", ExternalId = "people/c1" },
        Contact = new ContactItem
        {
            FirstName = "amy",
            Annotation = annotated ? new ContactAnnotation { FavoriteOrder = 1 } : null,
        },
    };

    private static async Task<Dictionary<string, string>> AnnotationsFor(string cachedNames, Item? item = null)
    {
        item ??= Linked();

        var client = new FakeMailboxStoreClient
        {
            OnGetSyncState = _ => new SyncState
            {
                UserId = User,
                DeviceId = Device,
                CollectionId = Collection,
                SyncKey = "1",
                Watermark = 5,
                CachedAnnotationNames = cachedNames,
                PreviousSyncKey = "0",
                PreviousWatermark = 0,
            },
            OnGetItemEvents = _ => [new ItemEvent { Id = 6, CommitId = 6, ServerId = ServerId, EventType = ChangeEventType.Add }],
            OnGetItems = _ => [item],
            OnUpsertSyncState = _ => new SyncState { UserId = User, DeviceId = Device, CollectionId = Collection },
        };

        var response = await NewService(client).SyncAsync(User, Device, new SyncCollection
        {
            CollectionId = Collection,
            SyncKey = "1",
            GetChanges = true,
        });

        var data = response.Commands!.Add.Single(a => a.ServerId == ServerId).ApplicationData;
        var annotations = data.Elements.SingleOrDefault(e => e.LocalName == "Annotations");
        if (annotations is null)
            return [];

        return annotations.ChildNodes.Cast<System.Xml.XmlNode>().ToDictionary(
            n => n.SelectSingleNode("*[local-name()='Name']")!.InnerText,
            n => n.SelectSingleNode("*[local-name()='Value']")?.InnerText ?? string.Empty);
    }

    [Fact]
    public async Task The_81_annotation_set_gets_a_suffixed_passport_mri()
    {
        var annotations = await AnnotationsFor(EightOneNames);

        Assert.Equal("1:amy@relivewp.net", annotations["MRI_0"]);
        Assert.False(annotations.ContainsKey("MRI"), "MRI goes out suffixed, never bare");
    }

    [Fact]
    public async Task Booleans_go_out_as_True_not_1()
    {
        var annotations = await AnnotationsFor(EightOneNames);

        Assert.Equal("True", annotations["MobileIMEnabled"]);
    }

    [Fact]
    public async Task The_81_set_carries_the_new_names_and_not_immri()
    {
        var annotations = await AnnotationsFor(EightOneNames);

        Assert.Equal("WL", annotations["SID"]);
        Assert.Equal("Regular", annotations["ShellContactType"]);

        // 8.1 never asks for IMMRI, so it must not appear even though the contact has one
        Assert.False(annotations.ContainsKey("IMMRI"));
    }

    [Fact]
    public async Task Origin_goes_out_only_to_a_client_that_asked_for_it()
    {
        var phone = await AnnotationsFor(EightOneNames, Mirrored(annotated: true));
        Assert.False(phone.ContainsKey("OriginService"));
        Assert.False(phone.ContainsKey("OriginCollection"));

        var web = await AnnotationsFor("CID,OriginService,OriginCollection", Mirrored(annotated: true));
        Assert.Equal("google", web["OriginService"]);
        Assert.Equal("people/me/connections", web["OriginCollection"]);
    }

    [Fact]
    public async Task A_mirrored_contact_with_no_annotation_row_still_carries_its_origin()
    {
        var web = await AnnotationsFor("CID,OriginService,OriginCollection", Mirrored(annotated: false));

        Assert.Equal("google", web["OriginService"]);
        Assert.False(web.ContainsKey("CID"));
    }

    [Fact]
    public async Task The_wp7_set_still_gets_immri_and_no_mri()
    {
        var annotations = await AnnotationsFor("CID,OID,WLID,IMMRI,Type,UserTileUrl");

        Assert.Equal("1:amy@relivewp.net", annotations["IMMRI"]);
        Assert.DoesNotContain(annotations.Keys, k => k.StartsWith("MRI"));
        Assert.False(annotations.ContainsKey("SID"));
    }
}
