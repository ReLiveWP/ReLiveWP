using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;
using ProtoFolderType = ReLiveWP.Services.Grpc.Mailbox.FolderType;

namespace ReLiveWP.Services.Exchange.Tests;

// folder 27 reports Class=Contacts and holds the me contact, so a WP7 device that saw it would sync
// it as an ordinary contacts collection and end up with two "me"s. 8.1 asks for
// SID/AN/Permission/InternalFolderType at FolderSync, WP7 asks for SID and not the latter two.
public class MeContactFolderGateTests
{
    private const string User = "u1";
    private const string Device = "d1";

    private static readonly string[] EightOneNames = ["SID", "AN", "Permission", "InternalFolderType"];
    private static readonly string[] Wp7Names = ["SID", "AN", "DomainId"];

    private static FolderSyncService NewService(FakeMailboxStoreClient client) =>
        new(client, new OrphanFolderTracker(), NullLogger<FolderSyncService>.Instance,
            Options.Create(new EasSyncOptions { ServeMeContactFolder = true }));

    private static FakeMailboxStoreClient Client(List<UpsertSyncStateRequest>? written = null) => new()
    {
        OnGetSyncState = _ => throw new RpcException(new Status(StatusCode.NotFound, "")),
        OnGetFolderEventTip = _ => new Watermark { Value = 3 },
        OnListFolders = _ =>
        [
            new Folder { Id = "contacts", DisplayName = "Contacts", Type = ProtoFolderType.ContactsDefault },
            new Folder { Id = "me", DisplayName = "MeContact", Type = ProtoFolderType.MeContact, SourceId = "ABCH" },
        ],
        OnUpsertSyncState = req =>
        {
            written?.Add(req);
            return new SyncState { UserId = User, DeviceId = Device, CollectionId = "0" };
        },
    };

    private static async Task<List<string>> FolderIdsFor(string[] annotationNames)
    {
        var response = await NewService(Client()).SyncAsync(
            User, Device, "0", annotationNames.ToHashSet(StringComparer.Ordinal));

        return [.. response.Changes!.Add.Select(a => a.ServerId)];
    }

    [Fact]
    public async Task An_81_folder_sync_sees_the_me_contact_folder()
    {
        Assert.Contains("me", await FolderIdsFor(EightOneNames));
    }

    [Fact]
    public async Task A_wp7_folder_sync_does_not()
    {
        var ids = await FolderIdsFor(Wp7Names);

        Assert.DoesNotContain("me", ids);
        Assert.Contains("contacts", ids);
    }

    [Fact]
    public async Task Permission_alone_is_enough()
    {
        Assert.Contains("me", await FolderIdsFor(["SID", "Permission"]));
    }

    [Fact]
    public async Task A_client_asking_for_no_annotations_at_all_does_not_see_it()
    {
        Assert.DoesNotContain("me", await FolderIdsFor([]));
    }

    // desktop Windows Mail ignores folder type 27 and nothing fills DbNetwork yet, so it is off
    // until someone turns it on to try the phone
    [Fact]
    public async Task The_folder_stays_hidden_while_the_option_is_off()
    {
        var service = new FolderSyncService(Client(), new OrphanFolderTracker(),
            NullLogger<FolderSyncService>.Instance, Options.Create(new EasSyncOptions()));

        var response = await service.SyncAsync(
            User, Device, "0", EightOneNames.ToHashSet(StringComparer.Ordinal));

        var ids = response.Changes!.Add.Select(a => a.ServerId).ToList();
        Assert.DoesNotContain("me", ids);
        Assert.Contains("contacts", ids);
    }

    [Fact]
    public async Task The_requested_names_are_stored_so_the_next_sync_can_reuse_them()
    {
        var written = new List<UpsertSyncStateRequest>();
        await NewService(Client(written)).SyncAsync(
            User, Device, "0", EightOneNames.ToHashSet(StringComparer.Ordinal));

        var cached = written.Single(w => w.CollectionId == "0").CachedAnnotationNames;
        Assert.All(EightOneNames, n => Assert.Contains(n, cached));
    }
}
