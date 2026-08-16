using Microsoft.Extensions.Logging.Abstractions;
using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Services;
using ReLiveWP.Backend.Mailbox.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mailbox.Tests;

// GetFolder used to be the one read path that didn't filter DeletedAt; ItemSyncService relies on it
// throwing NotFound for soft-deleted folders to detect stale device collections.
public class MailboxStoreServiceTests : IDisposable
{
    private const string FolderId = "inbox-folder";
    private const string UserId = "user-1";

    private readonly SqliteConnection _connection;

    public MailboxStoreServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated();
        db.Folders.Add(new DbFolder
        {
            Id = FolderId,
            UserId = UserId,
            DisplayName = "Inbox",
            Type = DbFolderType.InboxDefault,
        });
        db.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private MailboxDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<MailboxDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new MailboxDbContext(options);
    }

    private MailboxStoreService NewService(MailboxDbContext db) =>
        new(db, new MailboxIntegrityService(db), new SyncStateRepairService(db), new MailboxDeletionService(db), FakeUserClient.NoLinks(db));

    private static ServerCallContext NewCallContext() => new StubCallContext();

    [Fact]
    public async Task GetFolder_returns_a_live_folder()
    {
        await using var db = NewContext();
        var folder = await NewService(db).GetFolder(
            new GetFolderRequest { UserId = UserId, ServerId = FolderId }, NewCallContext());

        Assert.Equal(FolderId, folder.Id);
    }

    [Fact]
    public async Task GetFolder_treats_a_soft_deleted_folder_as_not_found()
    {
        await using (var db = NewContext())
        {
            var folder = await db.Folders.SingleAsync(f => f.Id == FolderId);
            folder.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        await using var verify = NewContext();
        var ex = await Assert.ThrowsAsync<RpcException>(() => NewService(verify).GetFolder(
            new GetFolderRequest { UserId = UserId, ServerId = FolderId }, NewCallContext()));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task GetFolder_treats_a_missing_folder_as_not_found()
    {
        await using var db = NewContext();
        var ex = await Assert.ThrowsAsync<RpcException>(() => NewService(db).GetFolder(
            new GetFolderRequest { UserId = UserId, ServerId = "no-such-folder" }, NewCallContext()));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    private const string DstFolderId = "dst-folder";
    private static readonly byte[] ConversationId = [0x01, 0x02, 0x03, 0x04];

    private async Task SeedDstFolderAsync()
    {
        await using var db = NewContext();
        db.Folders.Add(new DbFolder
        {
            Id = DstFolderId,
            UserId = UserId,
            DisplayName = "Archive",
            Type = DbFolderType.Generic,
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedConversationEmailsAsync(int count, byte[]? conversationId = null)
    {
        await using var db = NewContext();
        for (var i = 0; i < count; i++)
        {
            db.Items.Add(new DbEmail
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = UserId,
                CollectionId = FolderId,
                ServerId = Guid.NewGuid().ToString("N"),
                ConversationId = conversationId ?? ConversationId,
                Subject = $"msg {i}",
            });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task MoveConversation_moves_every_email_sharing_the_conversation_id()
    {
        await SeedDstFolderAsync();
        await SeedConversationEmailsAsync(3);
        // an unrelated conversation in the same folder must be left alone
        await SeedConversationEmailsAsync(1, conversationId: [0xFF]);

        await using var db = NewContext();
        var reply = await NewService(db).MoveConversation(new MoveConversationRequest
        {
            UserId = UserId,
            ConversationId = Google.Protobuf.ByteString.CopyFrom(ConversationId),
            DstCollectionId = DstFolderId,
        }, NewCallContext());

        Assert.Equal(MoveItemStatus.MoveSuccess, reply.Status);
        Assert.Equal(3, reply.ItemsMoved);

        await using var verify = NewContext();
        var moved = await verify.Items.OfType<DbEmail>()
            .Where(e => e.ConversationId == ConversationId).ToListAsync();
        Assert.Equal(3, moved.Count);
        Assert.All(moved, e => Assert.Equal(DstFolderId, e.CollectionId));

        var untouched = await verify.Items.OfType<DbEmail>()
            .SingleAsync(e => e.ConversationId == new byte[] { 0xFF });
        Assert.Equal(FolderId, untouched.CollectionId);
    }

    [Fact]
    public async Task MoveConversation_with_unknown_conversation_id_returns_invalid_source()
    {
        await SeedDstFolderAsync();
        await SeedConversationEmailsAsync(1);

        await using var db = NewContext();
        var reply = await NewService(db).MoveConversation(new MoveConversationRequest
        {
            UserId = UserId,
            ConversationId = Google.Protobuf.ByteString.CopyFrom([0x99, 0x99]),
            DstCollectionId = DstFolderId,
        }, NewCallContext());

        Assert.Equal(MoveItemStatus.MoveInvalidSource, reply.Status);
        Assert.Equal(0, reply.ItemsMoved);
    }

    [Fact]
    public async Task MoveConversation_to_a_missing_folder_returns_invalid_dest()
    {
        await SeedConversationEmailsAsync(1);

        await using var db = NewContext();
        var reply = await NewService(db).MoveConversation(new MoveConversationRequest
        {
            UserId = UserId,
            ConversationId = Google.Protobuf.ByteString.CopyFrom(ConversationId),
            DstCollectionId = "no-such-folder",
        }, NewCallContext());

        Assert.Equal(MoveItemStatus.MoveInvalidDest, reply.Status);
    }

    [Fact]
    public async Task MoveConversation_already_entirely_in_dest_returns_same_collection()
    {
        await SeedDstFolderAsync();
        await SeedConversationEmailsAsync(2);

        await using (var db = NewContext())
        {
            var reply = await NewService(db).MoveConversation(new MoveConversationRequest
            {
                UserId = UserId,
                ConversationId = Google.Protobuf.ByteString.CopyFrom(ConversationId),
                DstCollectionId = DstFolderId,
            }, NewCallContext());
            Assert.Equal(MoveItemStatus.MoveSuccess, reply.Status);
        }

        // now every message in the conversation already lives in DstFolderId
        await using var db2 = NewContext();
        var repeat = await NewService(db2).MoveConversation(new MoveConversationRequest
        {
            UserId = UserId,
            ConversationId = Google.Protobuf.ByteString.CopyFrom(ConversationId),
            DstCollectionId = DstFolderId,
        }, NewCallContext());

        Assert.Equal(MoveItemStatus.MoveSameCollection, repeat.Status);
    }

    [Fact]
    public async Task DeleteMailbox_rejects_an_empty_UserId()
    {
        await using var db = NewContext();
        var ex = await Assert.ThrowsAsync<RpcException>(() => NewService(db).DeleteMailbox(
            new DeleteMailboxRequest { UserId = "" }, NewCallContext()));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    [Fact]
    public async Task DeleteMailbox_reports_found_and_deletes_the_folder()
    {
        await using (var db = NewContext())
        {
            var result = await NewService(db).DeleteMailbox(
                new DeleteMailboxRequest { UserId = UserId }, NewCallContext());

            Assert.True(result.Found);
            Assert.Equal(1, result.FoldersDeleted);
        }

        await using var verify = NewContext();
        Assert.Empty(await verify.Folders.Where(f => f.UserId == UserId).ToListAsync());
    }

    [Fact]
    public async Task DeleteMailbox_on_an_unknown_user_reports_not_found()
    {
        await using var db = NewContext();
        var result = await NewService(db).DeleteMailbox(
            new DeleteMailboxRequest { UserId = "no-such-user" }, NewCallContext());

        Assert.False(result.Found);
        Assert.Equal(0, result.FoldersDeleted);
        Assert.Equal(0, result.ItemsDeleted);
    }
}
