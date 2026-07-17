using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Services;

namespace ReLiveWP.Backend.Mailbox.Tests;

public class MailboxRetentionSweepServiceTests : IDisposable
{
    private const string UserId = "user-1";
    private const string FolderId = "folder-1";
    private static readonly TimeSpan RetentionWindow = TimeSpan.FromDays(30);

    private readonly SqliteConnection _connection;

    public MailboxRetentionSweepServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private MailboxDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<MailboxDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new MailboxDbContext(options);
    }

    [Fact]
    public async Task SweepAsync_purges_soft_deleted_items_past_the_retention_window()
    {
        await using (var db = NewContext())
        {
            db.Folders.Add(new DbFolder { Id = FolderId, UserId = UserId, DisplayName = "Tasks", Type = DbFolderType.TasksDefault });
            db.Items.Add(new DbTask
            {
                Id = "old", ServerId = "old", UserId = UserId, CollectionId = FolderId,
                DeletedAt = DateTime.UtcNow - RetentionWindow - TimeSpan.FromDays(1),
            });
            db.Items.Add(new DbTask
            {
                Id = "recent", ServerId = "recent", UserId = UserId, CollectionId = FolderId,
                DeletedAt = DateTime.UtcNow - TimeSpan.FromDays(1),
            });
            db.Items.Add(new DbTask { Id = "live", ServerId = "live", UserId = UserId, CollectionId = FolderId });
            await db.SaveChangesAsync();
        }

        await using (var db = NewContext())
        {
            var result = await new MailboxRetentionSweepService(db).SweepAsync(RetentionWindow);
            Assert.Equal(1, result.Items);
        }

        await using var verify = NewContext();
        var remaining = await verify.Items.Select(i => i.Id).ToListAsync();
        Assert.Equal(["live", "recent"], remaining.OrderBy(x => x));
    }

    [Fact]
    public async Task SweepAsync_purges_soft_deleted_folders_past_the_retention_window()
    {
        await using (var db = NewContext())
        {
            db.Folders.Add(new DbFolder
            {
                Id = "old-folder", UserId = UserId, DisplayName = "Old", Type = DbFolderType.Generic,
                DeletedAt = DateTime.UtcNow - RetentionWindow - TimeSpan.FromDays(1),
            });
            db.Folders.Add(new DbFolder { Id = FolderId, UserId = UserId, DisplayName = "Live", Type = DbFolderType.Generic });
            await db.SaveChangesAsync();
        }

        await using (var db = NewContext())
        {
            var result = await new MailboxRetentionSweepService(db).SweepAsync(RetentionWindow);
            Assert.Equal(1, result.Folders);
        }

        await using var verify = NewContext();
        Assert.Equal([FolderId], await verify.Folders.Select(f => f.Id).ToListAsync());
    }

    [Fact]
    public async Task SweepAsync_prunes_ItemEvents_below_the_live_watermark_floor()
    {
        await using (var db = NewContext())
        {
            db.SyncStates.Add(new DbSyncState
            {
                UserId = UserId, DeviceId = "device-1", CollectionId = FolderId,
                Watermark = 10, PreviousWatermark = 10, LastSeenAt = DateTime.UtcNow,
            });
            for (var i = 1; i <= 15; i++)
                db.ItemEvents.Add(new DbItemEvent
                {
                    UserId = UserId, CollectionId = FolderId, ServerId = $"item-{i}",
                    EventType = DbChangeEventType.Add, OccurredAt = DateTime.UtcNow,
                });
            await db.SaveChangesAsync();
        }

        await using (var db = NewContext())
        {
            // ids are assigned sequentially by sqlite's rowid; the prune is strictly "< floor" (a
            // device at watermark 10 has already consumed event id 10, but we keep it anyway as
            // an extra safety margin), so ids 1-9 go and 10-15 survive
            var result = await new MailboxRetentionSweepService(db).SweepAsync(RetentionWindow);
            Assert.Equal(9, result.ItemEvents);
        }

        await using var verify = NewContext();
        var remainingIds = await verify.ItemEvents.OrderBy(e => e.Id).Select(e => e.Id).ToListAsync();
        Assert.Equal(6, remainingIds.Count);
        Assert.All(remainingIds, id => Assert.True(id >= 10));
    }

    [Fact]
    public async Task SweepAsync_respects_PreviousWatermark_as_the_conservative_floor()
    {
        await using (var db = NewContext())
        {
            // Watermark has advanced to 10, but PreviousWatermark (the one-deep rollback
            // checkpoint) is still 3: a retransmit could roll the device back there.
            db.SyncStates.Add(new DbSyncState
            {
                UserId = UserId, DeviceId = "device-1", CollectionId = FolderId,
                Watermark = 10, PreviousWatermark = 3, LastSeenAt = DateTime.UtcNow,
            });
            for (var i = 1; i <= 10; i++)
                db.ItemEvents.Add(new DbItemEvent
                {
                    UserId = UserId, CollectionId = FolderId, ServerId = $"item-{i}",
                    EventType = DbChangeEventType.Add, OccurredAt = DateTime.UtcNow,
                });
            await db.SaveChangesAsync();
        }

        await using (var db = NewContext())
        {
            var result = await new MailboxRetentionSweepService(db).SweepAsync(RetentionWindow);
            Assert.Equal(2, result.ItemEvents); // only ids 1 and 2 are below the floor of 3
        }

        await using var verify = NewContext();
        Assert.Equal(8, await verify.ItemEvents.CountAsync());
    }

    [Fact]
    public async Task SweepAsync_never_prunes_a_collection_with_no_live_SyncState()
    {
        await using (var db = NewContext())
        {
            for (var i = 1; i <= 5; i++)
                db.ItemEvents.Add(new DbItemEvent
                {
                    UserId = UserId, CollectionId = FolderId, ServerId = $"item-{i}",
                    EventType = DbChangeEventType.Add, OccurredAt = DateTime.UtcNow,
                });
            await db.SaveChangesAsync();
        }

        await using (var db = NewContext())
        {
            var result = await new MailboxRetentionSweepService(db).SweepAsync(RetentionWindow);
            Assert.Equal(0, result.ItemEvents);
        }

        await using var verify = NewContext();
        Assert.Equal(5, await verify.ItemEvents.CountAsync());
    }

    [Fact]
    public async Task SweepAsync_prunes_FolderEvents_using_the_hierarchy_SyncState_per_user()
    {
        await using (var db = NewContext())
        {
            db.SyncStates.Add(new DbSyncState
            {
                UserId = UserId, DeviceId = "device-1", CollectionId = DbSyncState.FolderHierarchyCollectionId,
                Watermark = 5, PreviousWatermark = 5, LastSeenAt = DateTime.UtcNow,
            });
            for (var i = 1; i <= 8; i++)
                db.FolderEvents.Add(new DbFolderEvent
                {
                    UserId = UserId, ServerId = $"folder-{i}",
                    EventType = DbChangeEventType.Add, OccurredAt = DateTime.UtcNow,
                });
            await db.SaveChangesAsync();
        }

        await using (var db = NewContext())
        {
            var result = await new MailboxRetentionSweepService(db).SweepAsync(RetentionWindow);
            Assert.Equal(4, result.FolderEvents);
        }

        await using var verify = NewContext();
        Assert.Equal(4, await verify.FolderEvents.CountAsync());
    }
}
