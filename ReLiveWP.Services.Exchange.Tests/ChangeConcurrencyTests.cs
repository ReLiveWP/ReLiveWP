using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;
using ProtoContactItem = ReLiveWP.Services.Grpc.Mailbox.ContactItem;

namespace ReLiveWP.Services.Exchange.Tests;

public class ChangeConcurrencyTests
{
    private const string User = "u1";
    private const string ServerId = "item-1";

    private static SyncChange ChangeWith(string firstName)
    {
        var doc = new System.Xml.XmlDocument();
        var el = doc.CreateElement("FirstName", Constants.Contacts);
        el.InnerText = firstName;

        return new SyncChange
        {
            ServerId = ServerId,
            ApplicationData = new ApplicationData { Elements = { el } },
        };
    }

    private static Item ItemAtVersion(uint version, string firstName = "Ada") => new()
    {
        Id = ServerId,
        UserId = User,
        ServerId = ServerId,
        CollectionId = "c1",
        Version = version,
        Contact = new ProtoContactItem { FirstName = firstName },
    };

    private static ItemSyncService NewService(FakeMailboxStoreClient client) =>
        new(client, NullLogger<ItemSyncService>.Instance);

    private static Task<int> Change(ItemSyncService svc, SyncChange change) =>
        svc.HandleChangeAsync(User, "Contact", change, new HashSet<string>(),
            SyncConflict.ServerWins, GhostingPolicy.PreserveAll, CancellationToken.None);

    [Fact]
    public async Task Change_sends_the_version_it_read()
    {
        uint? seenExpectedVersion = null;
        var client = new FakeMailboxStoreClient
        {
            OnGetItem = _ => ItemAtVersion(42),
            OnUpdateItem = req =>
            {
                seenExpectedVersion = req.HasExpectedVersion ? req.ExpectedVersion : null;
                return new MutationResult { Found = true };
            },
        };

        var status = await Change(NewService(client), ChangeWith("Ada B"));

        Assert.Equal(1, status);
        Assert.Equal(42u, seenExpectedVersion);
    }

    [Fact]
    public async Task Losing_the_race_once_re_reads_and_succeeds()
    {
        int reads = 0, writes = 0;
        var client = new FakeMailboxStoreClient
        {
            // the row moves on between the first read and the retry
            OnGetItem = _ => ItemAtVersion((uint)(++reads)),
            OnUpdateItem = _ => new MutationResult { Found = true, Conflict = ++writes == 1 },
        };

        var status = await Change(NewService(client), ChangeWith("Ada B"));

        Assert.Equal(1, status);
        Assert.Equal(2, reads);
        Assert.Equal(2, writes);
    }

    [Fact]
    public async Task The_retry_re_reads_rather_than_resending_the_stale_version()
    {
        var sentVersions = new List<uint>();
        int reads = 0, writes = 0;
        var client = new FakeMailboxStoreClient
        {
            OnGetItem = _ => ItemAtVersion((uint)(++reads) * 10),
            OnUpdateItem = req =>
            {
                sentVersions.Add(req.ExpectedVersion);
                return new MutationResult { Found = true, Conflict = ++writes == 1 };
            },
        };

        await Change(NewService(client), ChangeWith("Ada B"));

        Assert.Equal([10u, 20u], sentVersions);
    }

    // bounded, so a genuinely contended item resolves instead of spinning
    [Fact]
    public async Task Repeatedly_losing_the_race_reports_a_conflict()
    {
        int writes = 0;
        var client = new FakeMailboxStoreClient
        {
            OnGetItem = _ => ItemAtVersion(1),
            OnUpdateItem = _ => { writes++; return new MutationResult { Found = true, Conflict = true }; },
        };

        var status = await Change(NewService(client), ChangeWith("Ada B"));

        Assert.Equal(7, status); // MS-ASCMD Sync Status 7 - conflict
        Assert.Equal(3, writes);
    }

    [Fact]
    public async Task A_vanished_item_still_reports_not_found()
    {
        var client = new FakeMailboxStoreClient
        {
            OnGetItem = _ => ItemAtVersion(1),
            OnUpdateItem = _ => new MutationResult { Found = false },
        };

        Assert.Equal(8, await Change(NewService(client), ChangeWith("Ada B")));
    }

    // the pre-existing server-wins check fires before any of this, and must not start retrying
    [Fact]
    public async Task Server_wins_conflict_still_short_circuits_without_writing()
    {
        int writes = 0;
        var client = new FakeMailboxStoreClient
        {
            OnGetItem = _ => ItemAtVersion(1),
            OnUpdateItem = _ => { writes++; return new MutationResult { Found = true }; },
        };
        var svc = NewService(client);

        var status = await svc.HandleChangeAsync(User, "Contact", ChangeWith("Ada B"),
            new HashSet<string> { ServerId }, SyncConflict.ServerWins,
            GhostingPolicy.PreserveAll, CancellationToken.None);

        Assert.Equal(7, status);
        Assert.Equal(0, writes);
    }
}
