using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mailbox.Tests;

// the RPCs that touch many items at once. what matters to a device is the row state afterwards and
// exactly one change event per item it can see, so these pin both and stay indifferent to how the
// rows are found and marked.
public class BulkMutationTests : MailboxStoreTestBase
{
    private const string TasksFolderId = "tasks";
    private const string OtherTasksFolderId = "tasks-2";
    private const string InboxId = "inbox";
    private const string ArchiveId = "archive";

    public BulkMutationTests()
    {
        SeedFolderAsync(TasksFolderId, DbFolderType.TasksDefault).GetAwaiter().GetResult();
        SeedFolderAsync(OtherTasksFolderId, DbFolderType.Task).GetAwaiter().GetResult();
        SeedFolderAsync(InboxId, DbFolderType.InboxDefault).GetAwaiter().GetResult();
        SeedFolderAsync(ArchiveId, DbFolderType.Generic).GetAwaiter().GetResult();
    }

    private Task<string> SeedTaskAsync(string collectionId, string subject = "task", DateTime? deletedAt = null,
        string? clientId = null, DateTime? createdAt = null, string userId = UserId) =>
        SeedItemAsync(new DbTask
        {
            UserId = userId,
            CollectionId = collectionId,
            Subject = subject,
            DeletedAt = deletedAt,
            ClientId = clientId,
            CreatedAt = createdAt ?? DateTime.UtcNow,
        });

    private Task<string> SeedEmailAsync(string collectionId, byte[] conversationId, string subject = "mail") =>
        SeedItemAsync(new DbEmail
        {
            CollectionId = collectionId,
            Subject = subject,
            ConversationId = conversationId,
            Body = new string('x', 4096),
        });

    private async Task<DbItem> LoadItemAsync(string serverId)
    {
        await using var db = NewContext();
        return await db.Items.SingleAsync(i => i.ServerId == serverId);
    }

    private async Task<DbFolder> LoadFolderAsync(string id)
    {
        await using var db = NewContext();
        return await db.Folders.SingleAsync(f => f.Id == id);
    }

    [Fact]
    public async Task DeleteFolder_soft_deletes_the_folder_and_every_live_item_in_it()
    {
        var t1 = await SeedTaskAsync(TasksFolderId);
        var t2 = await SeedTaskAsync(TasksFolderId);
        var t3 = await SeedTaskAsync(TasksFolderId);
        var alreadyGone = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var t4 = await SeedTaskAsync(TasksFolderId, deletedAt: alreadyGone);
        var elsewhere = await SeedTaskAsync(OtherTasksFolderId);

        await using (var db = NewContext())
        {
            var result = await NewService(db).DeleteFolder(
                new DeleteFolderRequest { UserId = UserId, ServerId = TasksFolderId }, NewCallContext());
            Assert.True(result.Found);
        }

        Assert.NotNull((await LoadFolderAsync(TasksFolderId)).DeletedAt);
        Assert.NotNull((await LoadItemAsync(t1)).DeletedAt);
        Assert.NotNull((await LoadItemAsync(t2)).DeletedAt);
        Assert.NotNull((await LoadItemAsync(t3)).DeletedAt);
        Assert.Equal(alreadyGone, (await LoadItemAsync(t4)).DeletedAt);
        Assert.Null((await LoadItemAsync(elsewhere)).DeletedAt);

        Assert.Equal(3, await CountItemEventsAsync(DbChangeEventType.Delete, t1, t2, t3));
        Assert.Equal(0, await CountItemEventsAsync(DbChangeEventType.Delete, t4, elsewhere));
        Assert.Equal(1, await CountFolderEventsAsync(DbChangeEventType.Delete, TasksFolderId));
    }

    [Fact]
    public async Task DeleteFolder_twice_reports_not_found_and_emits_nothing_more()
    {
        var t1 = await SeedTaskAsync(TasksFolderId);

        await using (var db = NewContext())
            await NewService(db).DeleteFolder(new DeleteFolderRequest { UserId = UserId, ServerId = TasksFolderId }, NewCallContext());

        await using (var db = NewContext())
        {
            var result = await NewService(db).DeleteFolder(
                new DeleteFolderRequest { UserId = UserId, ServerId = TasksFolderId }, NewCallContext());
            Assert.False(result.Found);
        }

        Assert.Equal(1, await CountItemEventsAsync(DbChangeEventType.Delete, t1));
        Assert.Equal(1, await CountFolderEventsAsync(DbChangeEventType.Delete, TasksFolderId));
    }

    [Fact]
    public async Task DeleteFolder_never_crosses_users()
    {
        await SeedFolderAsync("theirs", DbFolderType.TasksDefault, userId: OtherUserId);
        var theirs = await SeedTaskAsync("theirs", userId: OtherUserId);

        await using var db = NewContext();
        var result = await NewService(db).DeleteFolder(
            new DeleteFolderRequest { UserId = UserId, ServerId = "theirs" }, NewCallContext());

        Assert.False(result.Found);
        Assert.Null((await LoadItemAsync(theirs)).DeletedAt);
        Assert.Null((await LoadFolderAsync("theirs")).DeletedAt);
    }

    [Fact]
    public async Task EmptyFolder_without_subfolders_clears_only_the_folder_itself()
    {
        await SeedFolderAsync("child", DbFolderType.Task, parentId: TasksFolderId);
        var t1 = await SeedTaskAsync(TasksFolderId);
        var t2 = await SeedTaskAsync(TasksFolderId);
        var c1 = await SeedTaskAsync("child");

        await using (var db = NewContext())
        {
            var result = await NewService(db).EmptyFolder(
                new EmptyFolderRequest { UserId = UserId, CollectionId = TasksFolderId }, NewCallContext());
            Assert.True(result.Found);
            Assert.Equal(2, result.ItemsDeleted);
        }

        Assert.Null((await LoadFolderAsync(TasksFolderId)).DeletedAt);
        Assert.Null((await LoadFolderAsync("child")).DeletedAt);
        Assert.NotNull((await LoadItemAsync(t1)).DeletedAt);
        Assert.NotNull((await LoadItemAsync(t2)).DeletedAt);
        Assert.Null((await LoadItemAsync(c1)).DeletedAt);

        Assert.Equal(2, await CountItemEventsAsync(DbChangeEventType.Delete, t1, t2, c1));
        Assert.Equal(0, await CountFolderEventsAsync(DbChangeEventType.Delete, TasksFolderId, "child"));
    }

    [Fact]
    public async Task EmptyFolder_with_subfolders_recurses_and_soft_deletes_the_subfolders()
    {
        await SeedFolderAsync("child", DbFolderType.Task, parentId: TasksFolderId);
        await SeedFolderAsync("grandchild", DbFolderType.Task, parentId: "child");
        var t1 = await SeedTaskAsync(TasksFolderId);
        var t2 = await SeedTaskAsync(TasksFolderId);
        var c1 = await SeedTaskAsync("child");
        var g1 = await SeedTaskAsync("grandchild");
        var alreadyGone = await SeedTaskAsync("grandchild", deletedAt: DateTime.UtcNow);
        var elsewhere = await SeedTaskAsync(OtherTasksFolderId);

        await using (var db = NewContext())
        {
            var result = await NewService(db).EmptyFolder(
                new EmptyFolderRequest { UserId = UserId, CollectionId = TasksFolderId, DeleteSubFolders = true }, NewCallContext());
            Assert.True(result.Found);
            Assert.Equal(4, result.ItemsDeleted);
        }

        Assert.Null((await LoadFolderAsync(TasksFolderId)).DeletedAt);
        Assert.NotNull((await LoadFolderAsync("child")).DeletedAt);
        Assert.NotNull((await LoadFolderAsync("grandchild")).DeletedAt);
        foreach (var serverId in new[] { t1, t2, c1, g1 })
            Assert.NotNull((await LoadItemAsync(serverId)).DeletedAt);
        Assert.Null((await LoadItemAsync(elsewhere)).DeletedAt);

        Assert.Equal(4, await CountItemEventsAsync(DbChangeEventType.Delete, t1, t2, c1, g1));
        Assert.Equal(0, await CountItemEventsAsync(DbChangeEventType.Delete, alreadyGone, elsewhere));
        Assert.Equal(2, await CountFolderEventsAsync(DbChangeEventType.Delete, "child", "grandchild"));
        Assert.Equal(0, await CountFolderEventsAsync(DbChangeEventType.Delete, TasksFolderId));
    }

    [Fact]
    public async Task EmptyFolder_on_a_large_folder_never_loads_the_items()
    {
        const int total = 2500;
        var serverIds = new List<string>(total);
        await using (var db = NewContext())
        {
            for (var i = 0; i < total; i++)
            {
                var id = NewId();
                serverIds.Add(id);
                db.Items.Add(new DbEmail
                {
                    Id = id,
                    ServerId = id,
                    UserId = UserId,
                    CollectionId = InboxId,
                    Subject = $"mail {i}",
                    Body = new string('x', 2048),
                });
            }
            await db.SaveChangesAsync();
        }
        ResetSelectCount();

        await using (var db = NewContext())
        {
            var result = await NewService(db).EmptyFolder(
                new EmptyFolderRequest { UserId = UserId, CollectionId = InboxId }, NewCallContext());
            Assert.True(result.Found);
            Assert.Equal(total, result.ItemsDeleted);
        }

        // the folder row, then one id projection; no item ever becomes an entity
        Assert.Equal(2, SelectsExecuted);

        await using var verify = NewContext();
        Assert.Equal(0, await verify.Items.CountAsync(i => i.CollectionId == InboxId && i.DeletedAt == null));
        Assert.Equal(total, await verify.ItemEvents.CountAsync(e => e.CollectionId == InboxId && e.EventType == DbChangeEventType.Delete));
        Assert.Equal(total, await verify.ItemEvents.Where(e => e.CollectionId == InboxId && e.EventType == DbChangeEventType.Delete)
            .Select(e => e.ServerId).Distinct().CountAsync());
    }

    [Fact]
    public async Task EmptyFolder_on_an_unknown_or_deleted_folder_is_not_found()
    {
        await using (var db = NewContext())
        {
            var result = await NewService(db).EmptyFolder(
                new EmptyFolderRequest { UserId = UserId, CollectionId = "nope" }, NewCallContext());
            Assert.False(result.Found);
        }

        await using (var db = NewContext())
            await NewService(db).DeleteFolder(new DeleteFolderRequest { UserId = UserId, ServerId = TasksFolderId }, NewCallContext());

        await using (var db = NewContext())
        {
            var result = await NewService(db).EmptyFolder(
                new EmptyFolderRequest { UserId = UserId, CollectionId = TasksFolderId }, NewCallContext());
            Assert.False(result.Found);
        }
    }

    [Fact]
    public async Task MoveConversation_emits_a_delete_at_the_source_and_an_add_at_the_destination_per_email()
    {
        byte[] conversation = [1, 2, 3];
        var m1 = await SeedEmailAsync(InboxId, conversation);
        var m2 = await SeedEmailAsync(InboxId, conversation);
        var other = await SeedEmailAsync(InboxId, [9]);

        await using (var db = NewContext())
        {
            var result = await NewService(db).MoveConversation(new MoveConversationRequest
            {
                UserId = UserId,
                ConversationId = ByteString.CopyFrom(conversation),
                DstCollectionId = ArchiveId,
            }, NewCallContext());
            Assert.Equal(MoveItemStatus.MoveSuccess, result.Status);
            Assert.Equal(2, result.ItemsMoved);
        }

        foreach (var serverId in new[] { m1, m2 })
        {
            var events = await ItemEventsForAsync(serverId);
            Assert.Equal(3, events.Count);
            Assert.Equal((DbChangeEventType.Delete, InboxId), (events[1].EventType, events[1].CollectionId));
            Assert.Equal((DbChangeEventType.Add, ArchiveId), (events[2].EventType, events[2].CollectionId));
            Assert.Equal(ArchiveId, (await LoadItemAsync(serverId)).CollectionId);
        }

        Assert.Single(await ItemEventsForAsync(other));
        Assert.Equal(InboxId, (await LoadItemAsync(other)).CollectionId);
    }

    [Fact]
    public async Task MoveConversation_skips_emails_that_are_already_in_the_destination()
    {
        byte[] conversation = [1, 2, 3];
        var m1 = await SeedEmailAsync(InboxId, conversation);
        var m2 = await SeedEmailAsync(ArchiveId, conversation);

        await using (var db = NewContext())
        {
            var result = await NewService(db).MoveConversation(new MoveConversationRequest
            {
                UserId = UserId,
                ConversationId = ByteString.CopyFrom(conversation),
                DstCollectionId = ArchiveId,
            }, NewCallContext());
            Assert.Equal(MoveItemStatus.MoveSuccess, result.Status);
        }

        Assert.Equal(3, (await ItemEventsForAsync(m1)).Count);
        Assert.Single(await ItemEventsForAsync(m2));
    }

    [Fact]
    public async Task ReconcileDuplicateItems_keeps_the_client_id_holder_and_deletes_the_rest()
    {
        var early = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var loser = await SeedTaskAsync(TasksFolderId, "dup", createdAt: early);
        var winner = await SeedTaskAsync(TasksFolderId, "dup", clientId: "c-1", createdAt: early.AddDays(1));
        var unrelated = await SeedTaskAsync(TasksFolderId, "other");
        var sameSubjectElsewhere = await SeedTaskAsync(OtherTasksFolderId, "dup");

        await using (var db = NewContext())
        {
            var result = await NewService(db).ReconcileDuplicateItems(
                new ReconcileDuplicateItemsRequest { UserId = UserId }, NewCallContext());
            Assert.Equal(1, result.GroupsCollapsed);
            Assert.Equal(1, result.ItemsDeleted);
        }

        Assert.NotNull((await LoadItemAsync(loser)).DeletedAt);
        Assert.Null((await LoadItemAsync(winner)).DeletedAt);
        Assert.Null((await LoadItemAsync(unrelated)).DeletedAt);
        Assert.Null((await LoadItemAsync(sameSubjectElsewhere)).DeletedAt);

        Assert.Equal(1, await CountItemEventsAsync(DbChangeEventType.Delete, loser));
        Assert.Equal(0, await CountItemEventsAsync(DbChangeEventType.Delete, winner, unrelated, sameSubjectElsewhere));
    }

    [Fact]
    public async Task ReconcileDuplicateItems_prefers_the_oldest_when_nothing_has_a_client_id()
    {
        var early = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var oldest = await SeedTaskAsync(TasksFolderId, "dup", createdAt: early);
        var newer = await SeedTaskAsync(TasksFolderId, "dup", createdAt: early.AddDays(1));
        var newest = await SeedTaskAsync(TasksFolderId, "dup", createdAt: early.AddDays(2));

        await using (var db = NewContext())
        {
            var result = await NewService(db).ReconcileDuplicateItems(
                new ReconcileDuplicateItemsRequest { UserId = UserId, CollectionId = TasksFolderId }, NewCallContext());
            Assert.Equal(1, result.GroupsCollapsed);
            Assert.Equal(2, result.ItemsDeleted);
        }

        Assert.Null((await LoadItemAsync(oldest)).DeletedAt);
        Assert.NotNull((await LoadItemAsync(newer)).DeletedAt);
        Assert.NotNull((await LoadItemAsync(newest)).DeletedAt);
    }
}
