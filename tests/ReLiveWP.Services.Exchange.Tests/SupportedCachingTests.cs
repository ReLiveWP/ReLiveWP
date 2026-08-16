using System.Xml;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Tests;

/// <summary>
/// MS-ASCMD 2.2.3.179 caches the Supported list for subsequent synchronizations, but it is the
/// SyncKey=0 request that defines it. A re-prime therefore has to replace the cached policy rather
/// than inherit the previous relationship's.
/// </summary>
public class SupportedCachingTests
{
    private const string User = "u1";
    private const string Device = "d1";
    private const string Collection = "c1";

    private static ItemSyncService NewService(FakeMailboxStoreClient client,
                                              bool absentSupportedClearsOmitted = false) =>
        new(client, NullLogger<ItemSyncService>.Instance,
            Options.Create(new EasSyncOptions { AbsentSupportedClearsOmitted = absentSupportedClearsOmitted }));

    private static SyncSupported Supported(params (string Ns, string Name)[] elements)
    {
        var doc = new XmlDocument();
        var s = new SyncSupported();
        foreach (var (ns, name) in elements)
            s.Elements.Add(doc.CreateElement(name, ns));
        return s;
    }

    // a relationship that was already primed with a declared list
    private static SyncState Primed(string supportedElements) => new()
    {
        UserId = User,
        DeviceId = Device,
        CollectionId = Collection,
        SyncKey = "1",
        Watermark = 0,
        CachedAnnotationNames = string.Empty,
        SupportedElements = supportedElements,
        PreviousSyncKey = "0",
        PreviousWatermark = 0,
    };

    private static async Task<UpsertSyncStateRequest> RePrimeAsync(SyncSupported? supported,
                                                                   string existing = "Contacts:CompanyName",
                                                                   bool flag = false)
    {
        UpsertSyncStateRequest? captured = null;
        var client = new FakeMailboxStoreClient
        {
            OnGetSyncState = _ => Primed(existing),
            OnUpsertSyncState = req => { captured = req; return new SyncState(); },
        };

        await NewService(client, flag).SyncAsync(User, Device, new SyncCollection
        {
            CollectionId = Collection,
            SyncKey = "0",
            Supported = supported,
        });

        Assert.NotNull(captured);
        return captured;
    }

    [Fact]
    public async Task Reprime_without_Supported_overwrites_the_cached_policy()
    {
        var captured = await RePrimeAsync(null);

        Assert.Equal(GhostingMode.GhostNone, GhostingPolicy.Parse(captured.SupportedElements).Mode);
    }

    [Fact]
    public async Task Reprime_with_empty_Supported_caches_ghost_all()
    {
        var captured = await RePrimeAsync(Supported());

        Assert.Equal(string.Empty, captured.SupportedElements);
        Assert.Equal(GhostingMode.GhostAll, GhostingPolicy.Parse(captured.SupportedElements).Mode);
    }

    [Fact]
    public async Task Reprime_with_declared_Supported_caches_the_list()
    {
        var captured = await RePrimeAsync(Supported((Constants.Contacts, "JobTitle")));

        var policy = GhostingPolicy.Parse(captured.SupportedElements);
        Assert.Equal(GhostingMode.Declared, policy.Mode);
        Assert.True(policy.ShouldClear(Constants.Contacts, "JobTitle"));
        Assert.False(policy.ShouldClear(Constants.Contacts, "CompanyName"));
    }

    // the cache records what the client declared, not the gated reading of it, so the flag can be
    // turned on later without every device having to re-prime
    [Fact]
    public async Task Cached_policy_is_the_ungated_declaration()
    {
        var off = await RePrimeAsync(null, flag: false);
        var on = await RePrimeAsync(null, flag: true);

        Assert.Equal(on.SupportedElements, off.SupportedElements);
        Assert.Equal(GhostingMode.GhostNone, GhostingPolicy.Parse(off.SupportedElements).Mode);
    }

    [Fact]
    public async Task Incremental_sync_carries_the_cached_policy_forward()
    {
        UpsertSyncStateRequest? captured = null;
        var client = new FakeMailboxStoreClient
        {
            OnGetSyncState = _ => Primed("Contacts:CompanyName"),
            OnUpsertSyncState = req => { captured = req; return new SyncState(); },
        };

        await NewService(client).SyncAsync(User, Device, new SyncCollection
        {
            CollectionId = Collection,
            SyncKey = "1",
            GetChanges = false,
        });

        Assert.Equal("Contacts:CompanyName", captured?.SupportedElements);
    }
}
