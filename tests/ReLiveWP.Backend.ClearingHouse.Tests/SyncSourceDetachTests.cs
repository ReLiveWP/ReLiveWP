using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.ClearingHouse.Data;
using ReLiveWP.Backend.ClearingHouse.Services.Mirror;
using ReLiveWP.Services.Grpc.Mailbox;

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
    private sealed class FakeMailbox : MailboxStore.MailboxStoreClient
    {
        public List<string> DeletedFolders { get; } = [];

        private static AsyncUnaryCall<T> Call<T>(T value) => new(
            Task.FromResult(value), Task.FromResult(new Metadata()), () => Status.DefaultSuccess,
            () => [], () => { });

        public override AsyncUnaryCall<DeleteItemsByOriginResult> DeleteItemsByOriginAsync(
            DeleteItemsByOriginRequest request, CallOptions options) =>
            Call(new DeleteItemsByOriginResult { ItemsDeleted = 3 });

        public override AsyncUnaryCall<MutationResult> DeleteFolderAsync(
            DeleteFolderRequest request, CallOptions options)
        {
            DeletedFolders.Add(request.ServerId);
            return Call(new MutationResult { Found = true });
        }
    }

    // the calendar mirror creates a folder per remote calendar, so unlinking with the data has to
    // take the folder too. leaving it behind is what stacks up a duplicate on every relink.
    [Fact]
    public async Task Unlinking_with_the_data_removes_the_folder_the_mirror_made()
    {
        var mailbox = new FakeMailbox();
        var source = Seed();
        source.Kind = MirrorKind.Calendar;
        source.FolderId = "folder-1";
        _db.SaveChanges();

        await new SyncSourceDetach(mailbox, _db, NullLogger<SyncSourceDetach>.Instance)
            .DeleteAndDetachAsync([source]);

        Assert.Equal(["folder-1"], mailbox.DeletedFolders);
        Assert.Empty(_db.SyncSources);
    }

    // contacts merge into the undeletable default folder and never record one, so nothing is removed
    [Fact]
    public async Task Unlinking_contacts_touches_no_folder()
    {
        var mailbox = new FakeMailbox();
        var source = Seed();

        await new SyncSourceDetach(mailbox, _db, NullLogger<SyncSourceDetach>.Instance)
            .DeleteAndDetachAsync([source]);

        Assert.Empty(mailbox.DeletedFolders);
    }

    // keeping the data means the events are yours now, and they need somewhere to live
    [Fact]
    public async Task Unlinking_but_keeping_the_data_leaves_the_folder_alone()
    {
        var mailbox = new FakeMailbox();
        var source = Seed();
        source.Kind = MirrorKind.Calendar;
        source.FolderId = "folder-1";
        _db.SaveChanges();

        await new SyncSourceDetach(mailbox, _db, NullLogger<SyncSourceDetach>.Instance)
            .DetachAsync([source]);

        Assert.Empty(mailbox.DeletedFolders);
        Assert.Empty(_db.SyncSources);
    }

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
