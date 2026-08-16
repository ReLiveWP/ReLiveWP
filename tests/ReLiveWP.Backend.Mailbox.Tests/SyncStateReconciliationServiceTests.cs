using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Services;

namespace ReLiveWP.Backend.Mailbox.Tests;

// SyncState has no FK to Folders, so rows survive the folders/devices they point at; covers the sweep
// that cleans them up. Every test seeds a DbDeviceInfo to keep the device-orphan branch inert, except
// the one test that deliberately exercises it.
public class SyncStateReconciliationServiceTests : IDisposable
{
    private const string LiveFolderId = "live-folder";
    private const string DeletedFolderId = "deleted-folder";
    private const string UserId = "user-1";
    private const string DeviceId = "device-1";

    private readonly SqliteConnection _connection;

    public SyncStateReconciliationServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated();
        db.Folders.AddRange(
            new DbFolder { Id = LiveFolderId, UserId = UserId, DisplayName = "Inbox", Type = DbFolderType.InboxDefault },
            new DbFolder
            {
                Id = DeletedFolderId,
                UserId = UserId,
                DisplayName = "Old",
                Type = DbFolderType.Generic,
                DeletedAt = DateTime.UtcNow,
            });
        db.DeviceInfos.Add(new DbDeviceInfo { UserId = UserId, DeviceId = DeviceId });
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

    private static DbSyncState NewState(string collectionId, string userId = UserId, string deviceId = DeviceId) => new()
    {
        UserId = userId,
        DeviceId = deviceId,
        CollectionId = collectionId,
        SyncKey = "3",
        Watermark = 12,
        LastSeenAt = DateTime.UtcNow,
    };

    private async Task SeedAsync(params object[] rows)
    {
        await using var db = NewContext();
        db.AddRange(rows);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Sweep_deletes_SyncState_for_hard_deleted_folder()
    {
        await SeedAsync(NewState("no-such-folder"));

        await using (var db = NewContext())
        {
            var states = await new SyncStateRepairService(db).SweepAsync(null, default);
            Assert.Equal(1, states);
        }

        await using var verify = NewContext();
        Assert.Empty(verify.SyncStates);
    }

    [Fact]
    public async Task Sweep_deletes_SyncState_for_soft_deleted_folder()
    {
        await SeedAsync(NewState(DeletedFolderId));

        await using (var db = NewContext())
        {
            var states = await new SyncStateRepairService(db).SweepAsync(null, default);
            Assert.Equal(1, states);
        }

        await using var verify = NewContext();
        Assert.Empty(verify.SyncStates);
    }

    [Fact]
    public async Task Sweep_ignores_hierarchy_sentinel_collection()
    {
        // "0" is the folder hierarchy sentinel; it has no DbFolder row and must never look orphaned
        await SeedAsync(NewState(DbSyncState.FolderHierarchyCollectionId));

        await using (var db = NewContext())
        {
            var states = await new SyncStateRepairService(db).SweepAsync(null, default);
            Assert.Equal(0, states);
        }

        await using var verify = NewContext();
        Assert.Single(verify.SyncStates);
    }

    [Fact]
    public async Task Sweep_leaves_healthy_SyncState_alone()
    {
        await SeedAsync(NewState(LiveFolderId));

        await using (var db = NewContext())
        {
            var states = await new SyncStateRepairService(db).SweepAsync(null, default);
            Assert.Equal(0, states);
        }

        await using var verify = NewContext();
        Assert.Single(verify.SyncStates);
    }

    [Fact]
    public async Task Sweep_is_idempotent()
    {
        await SeedAsync(NewState(DeletedFolderId), NewState(LiveFolderId));

        await using (var db = NewContext())
            await new SyncStateRepairService(db).SweepAsync(null, default);

        await using (var db = NewContext())
        {
            var states = await new SyncStateRepairService(db).SweepAsync(null, default);
            Assert.Equal(0, states); // nothing left to delete on a second pass
        }

        await using var verify = NewContext();
        Assert.Single(verify.SyncStates); // the healthy one survives
    }

    [Fact]
    public async Task Sweep_can_be_scoped_to_one_user()
    {
        await using (var db = NewContext())
        {
            db.DeviceInfos.Add(new DbDeviceInfo { UserId = "user-2", DeviceId = DeviceId });
            await db.SaveChangesAsync();
        }

        await SeedAsync(
            NewState("orphan-a"),
            NewState("orphan-b", userId: "user-2"));

        await using (var db = NewContext())
        {
            var states = await new SyncStateRepairService(db).SweepAsync(UserId, default);
            Assert.Equal(1, states); // only user-1's orphan
        }

        await using var verify = NewContext();
        var remaining = await verify.SyncStates.SingleAsync();
        Assert.Equal("user-2", remaining.UserId);
    }

    // a device id nothing provisioned is far more likely to be one the gateway parsed wrong than a
    // device that went away, and deleting its sync state costs the real device its mailbox
    [Fact]
    public async Task Sweep_keeps_SyncState_for_a_device_with_no_DeviceInfo_row()
    {
        await SeedAsync(NewState(LiveFolderId, deviceId: "ghost-device"));

        await using (var db = NewContext())
        {
            var states = await new SyncStateRepairService(db).SweepAsync(null, default);
            Assert.Equal(0, states);
        }

        await using var verify = NewContext();
        Assert.Equal("ghost-device", (await verify.SyncStates.SingleAsync()).DeviceId);
    }

    [Fact]
    public async Task Unknown_devices_are_reported_with_the_rows_they_still_hold()
    {
        await SeedAsync(NewState(LiveFolderId, deviceId: "ghost-device"));

        await using var db = NewContext();
        var unknown = await new SyncStateRepairService(db).UnknownDevicesAsync(null, default);

        var ghost = Assert.Single(unknown);
        Assert.Equal("ghost-device", ghost.DeviceId);
        Assert.Equal(UserId, ghost.UserId);
        Assert.Equal(1, ghost.Rows);
    }
}
