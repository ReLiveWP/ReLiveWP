using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.ClearingHouse.Data;
using ReLiveWP.Backend.ClearingHouse.Services.Mirror;
using ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Tests;

// A calendar folder is a type 13, which the phone is allowed to rename and delete, unlike the
// default Contacts folder the contact mirror writes into. Both of those have to survive a poll.
public class CalendarFolderResolverTests
{
    private sealed class FakeMailbox : MailboxStore.MailboxStoreClient
    {
        public Dictionary<string, Folder> Folders { get; } = [];
        public List<UpdateFolderRequest> Updates { get; } = [];
        public List<CreateFolderRequest> Creates { get; } = [];

        private static AsyncUnaryCall<T> Call<T>(T value) => new(
            Task.FromResult(value), Task.FromResult(new Metadata()), () => Status.DefaultSuccess,
            () => [], () => { });

        public override AsyncUnaryCall<Folder> GetFolderAsync(GetFolderRequest request, CallOptions options) =>
            Folders.TryGetValue(request.ServerId, out var folder)
                ? Call(folder)
                : throw new RpcException(new Status(StatusCode.NotFound, "Folder not found"));

        public override AsyncUnaryCall<Folder> CreateFolderAsync(CreateFolderRequest request, CallOptions options)
        {
            Creates.Add(request);

            var folder = new Folder
            {
                Id = $"folder-{Creates.Count}",
                UserId = request.UserId,
                DisplayName = request.DisplayName,
                Type = request.Type,
                ParentServerId = request.ParentServerId,
            };

            Folders[folder.Id] = folder;
            return Call(folder);
        }

        public override AsyncUnaryCall<MutationResult> UpdateFolderAsync(
            UpdateFolderRequest request, CallOptions options)
        {
            Updates.Add(request);
            Folders[request.ServerId].DisplayName = request.DisplayName;
            return Call(new MutationResult { Found = true });
        }
    }

    private static DbSyncSource Source(string? folderId = null, string remote = "Work", string? pushed = null) => new()
    {
        Id = "src-1",
        UserId = "user-1",
        ConnectionId = "conn-1",
        Kind = MirrorKind.Calendar,
        ServiceId = "caldav",
        SourceId = "calendars/work/",
        SyncEnabled = true,
        FolderId = folderId,
        RemoteDisplayName = remote,
        FolderDisplayName = pushed,
    };

    private static CalendarFolderResolver Resolver(FakeMailbox mailbox) =>
        new(mailbox, NullLogger<CalendarFolderResolver>.Instance);

    [Fact]
    public async Task A_new_source_gets_a_type_13_folder_named_after_the_remote()
    {
        var mailbox = new FakeMailbox();
        var source = Source();

        var folderId = await Resolver(mailbox).ResolveAsync(source);

        var create = Assert.Single(mailbox.Creates);
        Assert.Equal(FolderType.Calendar, create.Type);
        Assert.Equal("Work", create.DisplayName);
        Assert.Equal("0", create.ParentServerId);
        Assert.Equal(folderId, source.FolderId);
        Assert.Equal("Work", source.FolderDisplayName);
    }

    [Fact]
    public async Task An_existing_folder_is_reused_without_touching_it()
    {
        var mailbox = new FakeMailbox();
        var source = Source();

        var first = await Resolver(mailbox).ResolveAsync(source);
        var second = await Resolver(mailbox).ResolveAsync(source);

        Assert.Equal(first, second);
        Assert.Single(mailbox.Creates);
        Assert.Empty(mailbox.Updates);
    }

    // deleting the folder on the phone is how you say you are done with that calendar. putting it
    // back would resurrect it on every poll, so the source is turned off instead.
    [Fact]
    public async Task A_folder_deleted_on_the_device_turns_the_source_off()
    {
        var mailbox = new FakeMailbox();
        var source = Source();

        await Resolver(mailbox).ResolveAsync(source);
        mailbox.Folders.Clear();

        await Assert.ThrowsAsync<MirrorException>(() => Resolver(mailbox).ResolveAsync(source).AsTask());

        Assert.False(source.SyncEnabled);
        Assert.Null(source.FolderId);
        Assert.Single(mailbox.Creates);
    }

    [Fact]
    public async Task A_rename_on_the_remote_is_pushed_to_the_folder()
    {
        var mailbox = new FakeMailbox();
        var source = Source();

        await Resolver(mailbox).ResolveAsync(source);
        source.RemoteDisplayName = "Work Calendar";

        await Resolver(mailbox).ResolveAsync(source);

        var update = Assert.Single(mailbox.Updates);
        Assert.Equal("Work Calendar", update.DisplayName);
        Assert.Equal(FolderType.Calendar, update.Type);
        Assert.Equal("Work Calendar", source.FolderDisplayName);
    }

    // the comparison is remote-name against last-pushed, never against the folder's live name, so a
    // rename made on the phone is not fought on the next poll
    [Fact]
    public async Task A_rename_on_the_device_sticks()
    {
        var mailbox = new FakeMailbox();
        var source = Source();

        var folderId = await Resolver(mailbox).ResolveAsync(source);
        mailbox.Folders[folderId].DisplayName = "My Diary";

        await Resolver(mailbox).ResolveAsync(source);

        Assert.Empty(mailbox.Updates);
        Assert.Equal("My Diary", mailbox.Folders[folderId].DisplayName);
    }

    [Fact]
    public async Task A_source_with_no_remote_name_falls_back_to_its_id()
    {
        var mailbox = new FakeMailbox();

        await Resolver(mailbox).ResolveAsync(Source(remote: ""));

        Assert.Equal("calendars/work/", Assert.Single(mailbox.Creates).DisplayName);
    }
}
