using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Services;
using ReLiveWP.Backend.Mailbox.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mailbox.Tests;

// covers UpsertSyncState's bounded reload-and-retry for a first-insert race (block H). Uses a
// shared-cache file-mode Sqlite db (rather than the usual single shared SqliteConnection) so two
// separate DbContext instances get separate connections and can genuinely interleave, unlike a
// single connection object which serializes every command onto it.
public class UpsertSyncStateConcurrencyTests : IDisposable
{
    private readonly SqliteConnection _keepAlive;
    private readonly string _connectionString;

    public UpsertSyncStateConcurrencyTests()
    {
        _connectionString = $"Data Source=file:racetest-{Guid.NewGuid():N}?mode=memory&cache=shared";
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();

        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    public void Dispose() => _keepAlive.Dispose();

    private MailboxDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<MailboxDbContext>()
            .UseSqlite(_connectionString)
            .Options;
        return new MailboxDbContext(options);
    }

    private MailboxStoreService NewService()
    {
        var db = NewContext();
        return new MailboxStoreService(db, new MailboxIntegrityService(db), new SyncStateRepairService(db), new MailboxDeletionService(db));
    }

    private static ServerCallContext NewCallContext() => new StubCallContext();

    [Fact]
    public async Task Concurrent_first_upserts_for_the_same_key_converge_to_a_single_row()
    {
        var request = new UpsertSyncStateRequest
        {
            UserId = "race-user",
            DeviceId = "race-device",
            CollectionId = "race-collection",
            SyncKey = "1",
            PreviousSyncKey = "0",
        };

        // two independent connections racing to insert the same (UserId, DeviceId, CollectionId);
        // whichever loses the unique-index race must retry as an update, not fail or duplicate
        var t1 = NewService().UpsertSyncState(request, NewCallContext());
        var t2 = NewService().UpsertSyncState(request, NewCallContext());
        await Task.WhenAll(t1, t2);

        await using var verify = NewContext();
        var rows = await verify.SyncStates.Where(s => s.UserId == "race-user").ToListAsync();
        Assert.Single(rows);
        Assert.Equal("1", rows[0].SyncKey);
    }
}
