using System.Xml;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;
using ProtoFolderType = ReLiveWP.Services.Grpc.Mailbox.FolderType;

namespace ReLiveWP.Services.Exchange.Tests;

// collection 27 has no real change tracking: one item, Add the first time and Change forever after,
// with the connected networks hung off it as suffixed annotations
public class MeContactCollectionTests
{
    private const string User = "u1";
    private const string Device = "d1";
    private const string Collection = "me";
    private const string MeServerId = "me-contact-1";

    private const string Names =
        "CID,WLID,Type,SID,NetworkSourceId,NetworkAccountName,NetworkPSAState,NetworkOffers,"
        + "NetworkClientToken2,DomainTag,NetworkLastSync";

    private static ItemSyncService NewService(FakeMailboxStoreClient client) =>
        new(client, NullLogger<ItemSyncService>.Instance,
            Options.Create(new EasSyncOptions { ServeMeContactFolder = true }));

    private static FakeMailboxStoreClient Client(long watermark,
                                                 IEnumerable<Network>? networks = null,
                                                 bool haveMeContact = true) => new()
    {
        OnGetFolder = req => new Folder { Id = req.ServerId, Type = ProtoFolderType.MeContact },
        OnGetSyncState = _ => new SyncState
        {
            UserId = User,
            DeviceId = Device,
            CollectionId = Collection,
            SyncKey = "1",
            Watermark = watermark,
            CachedAnnotationNames = Names,
        },
        OnGetMeContact = _ => haveMeContact
            ? MeContact()
            : throw new RpcException(new Status(StatusCode.NotFound, "no me contact")),
        OnListNetworks = _ => networks ?? [],
        OnUpsertSyncState = _ => new SyncState { UserId = User, DeviceId = Device, CollectionId = Collection },
    };

    private static Item MeContact() => new()
    {
        ServerId = MeServerId,
        CollectionId = "contacts",
        Contact = new ContactItem
        {
            FirstName = "wam",
            Email1Address = "wam@relivewp.net",
            Annotation = new ContactAnnotation
            {
                WlId = "wam@relivewp.net",
                ContactType = "Me",
                SourceId = "WL",
            },
        },
    };

    private static SyncCollection Request(string syncKey) => new()
    {
        CollectionId = Collection,
        SyncKey = syncKey,
        GetChanges = true,
    };

    private static Dictionary<string, string> AnnotationsOf(ApplicationData data)
    {
        var annotations = data.Elements.Single(e => e.LocalName == "Annotations");
        return annotations.ChildNodes.Cast<XmlNode>().ToDictionary(
            n => n.SelectSingleNode("*[local-name()='Name']")!.InnerText,
            n => n.SelectSingleNode("*[local-name()='Value']")?.InnerText ?? string.Empty);
    }

    [Fact]
    public async Task Priming_sends_nothing_and_leaves_the_unsent_marker()
    {
        UpsertSyncStateRequest? written = null;
        var client = Client(watermark: 0);
        client.OnGetSyncState = _ => throw new RpcException(new Status(StatusCode.NotFound, ""));
        client.OnUpsertSyncState = req =>
        {
            written = req;
            return new SyncState { UserId = User, DeviceId = Device, CollectionId = Collection };
        };

        var response = await NewService(client).SyncAsync(User, Device, Request("0"));

        Assert.Equal("1", response.SyncKey);
        Assert.Null(response.Commands);
        Assert.Equal(-1, written!.Watermark);
    }

    [Fact]
    public async Task The_first_content_sync_adds_exactly_one_item()
    {
        var response = await NewService(Client(watermark: -1)).SyncAsync(User, Device, Request("1"));

        var add = Assert.Single(response.Commands!.Add);
        Assert.Equal(MeServerId, add.ServerId);
        Assert.Empty(response.Commands.Change);
    }

    [Fact]
    public async Task Every_sync_after_that_changes_the_same_item()
    {
        var response = await NewService(Client(watermark: 0)).SyncAsync(User, Device, Request("1"));

        var change = Assert.Single(response.Commands!.Change);
        Assert.Equal(MeServerId, change.ServerId);
        Assert.Empty(response.Commands.Add);
    }

    [Fact]
    public async Task Networks_ride_on_the_me_contact_suffixed_by_row()
    {
        var networks = new[]
        {
            new Network { DomainId = 22, UserEmail = "wam@relivewp.net", AccountName = "wamwoowam", PsaState = "Accept", Offers = 2129 },
            new Network { DomainId = 7, UserEmail = "wam@relivewp.net", AccountName = "wam.fb" },
        };

        var response = await NewService(Client(watermark: 0, networks)).SyncAsync(User, Device, Request("1"));
        var annotations = AnnotationsOf(response.Commands!.Change.Single().ApplicationData);

        Assert.Equal("TWITR", annotations["NetworkSourceId0"]);
        Assert.Equal("wamwoowam", annotations["NetworkAccountName0"]);
        Assert.Equal("Accept", annotations["NetworkPSAState0"]);
        Assert.Equal("2129", annotations["NetworkOffers0"]);

        Assert.Equal("FB", annotations["NetworkSourceId1"]);
        Assert.Equal("wam.fb", annotations["NetworkAccountName1"]);
    }

    // NetworkClientToken2 already ends in a digit, so the row index needs a separator
    [Fact]
    public async Task A_name_ending_in_a_digit_takes_an_underscore()
    {
        var networks = new[] { new Network { DomainId = 7, UserEmail = "wam@relivewp.net", ClientToken2 = "tok" } };

        var response = await NewService(Client(watermark: 0, networks)).SyncAsync(User, Device, Request("1"));
        var annotations = AnnotationsOf(response.Commands!.Change.Single().ApplicationData);

        Assert.Equal("tok", annotations["NetworkClientToken2_0"]);
        Assert.False(annotations.ContainsKey("NetworkClientToken20"));
    }

    [Fact]
    public async Task The_contact_annotations_still_come_through_alongside()
    {
        var response = await NewService(Client(watermark: 0)).SyncAsync(User, Device, Request("1"));
        var annotations = AnnotationsOf(response.Commands!.Change.Single().ApplicationData);

        Assert.Equal("Me", annotations["Type"]);
        Assert.Equal("WL", annotations["SID"]);
    }

    [Fact]
    public async Task A_mailbox_with_no_me_contact_returns_an_empty_collection_rather_than_faulting()
    {
        var response = await NewService(Client(watermark: 0, haveMeContact: false))
            .SyncAsync(User, Device, Request("1"));

        Assert.Equal(1, response.Status);
        Assert.Null(response.Commands);
    }
}
