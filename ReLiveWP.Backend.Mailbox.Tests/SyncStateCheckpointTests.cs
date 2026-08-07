using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Services;
using ReLiveWP.Backend.Mailbox.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mailbox.Tests;

// the PreviousSyncKey/PreviousWatermark checkpoint must survive the store/read round-trip
public class SyncStateCheckpointTests : IDisposable
{
    private const string UserId = "user-1";
    private const string DeviceId = "device-1";
    private const string CollectionId = "collection-1";

    private readonly SqliteConnection _connection;

    public SyncStateCheckpointTests()
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

    private MailboxStoreService NewService(MailboxDbContext db) =>
        new(db, new MailboxIntegrityService(db), new SyncStateRepairService(db), new MailboxDeletionService(db));

    private static ServerCallContext NewCallContext() => new StubCallContext();

    [Fact]
    public async Task UpsertSyncState_persists_the_checkpoint_and_GetSyncState_returns_it()
    {
        await using (var db = NewContext())
        {
            await NewService(db).UpsertSyncState(new UpsertSyncStateRequest
            {
                UserId = UserId,
                DeviceId = DeviceId,
                CollectionId = CollectionId,
                SyncKey = "4",
                Watermark = 120,
                PreviousSyncKey = "3",
                PreviousWatermark = 90,
            }, NewCallContext());
        }

        await using (var db = NewContext())
        {
            var state = await NewService(db).GetSyncState(new GetSyncStateRequest
            {
                UserId = UserId,
                DeviceId = DeviceId,
                CollectionId = CollectionId,
            }, NewCallContext());

            Assert.Equal("4", state.SyncKey);
            Assert.Equal(120, state.Watermark);
            Assert.Equal("3", state.PreviousSyncKey);
            Assert.Equal(90, state.PreviousWatermark);
        }
    }

    [Fact]
    public async Task Upserting_the_same_state_row_overwrites_the_checkpoint()
    {
        await using (var db = NewContext())
        {
            var svc = NewService(db);
            await svc.UpsertSyncState(new UpsertSyncStateRequest
            {
                UserId = UserId,
                DeviceId = DeviceId,
                CollectionId = CollectionId,
                SyncKey = "4",
                Watermark = 120,
                PreviousSyncKey = "3",
                PreviousWatermark = 90,
            }, NewCallContext());
        }

        await using (var db = NewContext())
        {
            await NewService(db).UpsertSyncState(new UpsertSyncStateRequest
            {
                UserId = UserId,
                DeviceId = DeviceId,
                CollectionId = CollectionId,
                SyncKey = "5",
                Watermark = 150,
                PreviousSyncKey = "4",
                PreviousWatermark = 120,
            }, NewCallContext());
        }

        await using (var db = NewContext())
        {
            var state = await NewService(db).GetSyncState(new GetSyncStateRequest
            {
                UserId = UserId,
                DeviceId = DeviceId,
                CollectionId = CollectionId,
            }, NewCallContext());

            Assert.Equal("5", state.SyncKey);
            Assert.Equal("4", state.PreviousSyncKey);
            Assert.Equal(120, state.PreviousWatermark);
        }
    }
}
