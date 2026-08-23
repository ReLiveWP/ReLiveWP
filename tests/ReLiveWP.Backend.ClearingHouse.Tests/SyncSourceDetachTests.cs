using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.ClearingHouse.Data;
using ReLiveWP.Backend.ClearingHouse.Services.Mirror;

namespace ReLiveWP.Backend.ClearingHouse.Tests;

public class SyncSourceDetachTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ClearingHouseDbContext _db;

    public SyncSourceDetachTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _db = new ClearingHouseDbContext(
            new DbContextOptionsBuilder<ClearingHouseDbContext>().UseSqlite(_connection).Options);

        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private SyncSourceDetach Detach() =>
        new(null!, _db, NullLogger<SyncSourceDetach>.Instance);

    private DbSyncSource Seed(string sourceId = "card/", string connectionId = "conn-1")
    {
        var source = new DbSyncSource
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = "user-1",
            ConnectionId = connectionId,
            Kind = MirrorKind.Contacts,
            ServiceId = "carddav",
            SourceId = sourceId,
        };

        _db.SyncSources.Add(source);
        _db.SaveChanges();
        return source;
    }

    // detaching is what turns "kept in step with over there" into "yours". The contacts are somebody
    // else's table entirely, and the point is that nothing here touches them.
    [Fact]
    public async Task Detaching_removes_the_source()
    {
        var source = Seed();

        var count = await Detach().DetachAsync([source]);

        Assert.Equal(1, count);
        Assert.Empty(_db.SyncSources);
    }

    [Fact]
    public async Task Detaching_one_source_leaves_another_alone()
    {
        var first = Seed("card/");
        Seed("card/work/");

        await Detach().DetachAsync([first]);

        Assert.Single(_db.SyncSources);
        Assert.Equal("card/work/", _db.SyncSources.Single().SourceId);
    }

    [Fact]
    public async Task Detaching_nothing_is_not_an_error()
    {
        Seed();

        Assert.Equal(0, await Detach().DetachAsync([]));
        Assert.Single(_db.SyncSources);
    }

    [Fact]
    public async Task Every_source_on_a_connection_is_found()
    {
        Seed("card/", "conn-1");
        Seed("card/work/", "conn-1");
        Seed("card/other/", "conn-2");

        var found = await Detach().ForConnectionAsync("user-1", "conn-1");

        Assert.Equal(2, found.Count);
        Assert.All(found, s => Assert.Equal("conn-1", s.ConnectionId));
    }

    [Fact]
    public async Task Another_users_source_is_never_returned()
    {
        var mine = Seed();
        mine.UserId = "user-2";
        await _db.SaveChangesAsync();

        Assert.Empty(await Detach().ForConnectionAsync("user-1", "conn-1"));
    }
}
