using System.Security.Claims;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReLiveWP.Services.Exchange.Controllers;
using ReLiveWP.Services.Exchange.Middleware;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Tests;

// MS-ASCMD 2.2.1.21.1: an empty Sync body replays the cached request, and nothing else
public class SyncRequestCacheTests
{
    private const string User = "user-1";
    private const string Device = "device-1";
    private const string Inbox = "inbox";
    private const string Contacts = "contacts";
    private const string Forgotten = "forgotten";

    private sealed class FakeSyncRequestCache : ISyncRequestCache
    {
        public CachedSyncRequest? Entry { get; set; }

        public Task<CachedSyncRequest?> GetAsync(string userId, string deviceId, CancellationToken ct = default) =>
            Task.FromResult(Entry);

        public Task StoreAsync(string userId, string deviceId, CachedSyncRequest request, CancellationToken ct = default)
        {
            Entry = request;
            return Task.CompletedTask;
        }

        public Task DisarmAsync(string userId, string deviceId, CancellationToken ct = default)
        {
            if (Entry is not null) Entry = Entry with { Replayable = false };
            return Task.CompletedTask;
        }
    }

    private static SyncController CreateController(
        FakeMailboxStoreClient mailbox, ISyncRequestCache cache, out MemoryStream responseBody)
    {
        var itemSync = new ItemSyncService(mailbox, NullLogger<ItemSyncService>.Instance,
            Options.Create(new EasSyncOptions()));
        var monitor = new PushMonitor(mailbox, new MailboxChangeNotifier(null!, NullLogger<MailboxChangeNotifier>.Instance));

        var controller = new SyncController(NullLogger<SyncController>.Instance, itemSync, monitor, cache);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, User)])),
        };
        responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private static void SetBody(SyncController controller, string? collectionsXml, bool decodeFailed = false)
    {
        XmlDocument? doc = null;
        if (collectionsXml is not null)
        {
            doc = new XmlDocument();
            doc.LoadXml($"""
                <?xml version="1.0" encoding="utf-8"?>
                <Sync xmlns="AirSync" xmlns:airsyncbase="AirSyncBase" xmlns:wl="WindowsLive">
                  {collectionsXml}
                </Sync>
                """);
        }

        controller.HttpContext.Items[ActiveSyncMiddleware.ContextKey] = new ActiveSyncContext
        {
            Command = EasCommand.Sync,
            DeviceId = Device,
            XmlDocument = doc,
            BodyDecodeFailed = decodeFailed,
        };
    }

    private static XmlElement? DecodeResponse(MemoryStream responseBody)
    {
        var bytes = responseBody.ToArray();
        if (bytes.Length == 0) return null;

        var decoder = new ASWBXML();
        decoder.LoadBytes(bytes);
        return decoder.GetXmlDocument().DocumentElement;
    }

    private static IEnumerable<XmlElement> Children(XmlElement root, string localName) =>
        root.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == localName);

    private static string? Value(XmlElement root, string localName) =>
        Children(root, localName).FirstOrDefault()?.InnerText;

    private static SyncState State(string collectionId, string syncKey, string previousKey = "0",
                                   long watermark = 0, long previousWatermark = 0) => new()
                                   {
                                       UserId = User,
                                       DeviceId = Device,
                                       CollectionId = collectionId,
                                       SyncKey = syncKey,
                                       Watermark = watermark,
                                       PreviousSyncKey = previousKey,
                                       PreviousWatermark = previousWatermark,
                                   };

    private static CachedSyncCollection Cached(string collectionId, string syncKey) => new()
    {
        CollectionId = collectionId,
        SyncKey = syncKey,
        GetChanges = true,
    };

    [Fact]
    public async Task An_empty_body_with_no_cached_request_is_status_13()
    {
        var mailbox = new FakeMailboxStoreClient();
        var controller = CreateController(mailbox, new FakeSyncRequestCache(), out var body);
        SetBody(controller, null);

        await controller.Post();

        var response = DecodeResponse(body);
        Assert.NotNull(response);
        Assert.Equal("13", Value(response!, "Status"));
    }

    [Fact]
    public async Task An_empty_body_with_a_disarmed_cache_is_status_13()
    {
        var cache = new FakeSyncRequestCache
        {
            Entry = new CachedSyncRequest { Replayable = false, Collections = [Cached(Inbox, "5")] },
        };

        var controller = CreateController(new FakeMailboxStoreClient(), cache, out var body);
        SetBody(controller, null);

        await controller.Post();

        var response = DecodeResponse(body);
        Assert.NotNull(response);
        Assert.Equal("13", Value(response!, "Status"));
    }

    // the trace: client at 64, server at 65 because a response went missing. the old code synced
    // from the server's key and jumped to 66, stranding everything delivered at 65.
    [Fact]
    public async Task A_client_that_is_behind_the_server_is_told_to_resend_rather_than_skipped_past()
    {
        var writes = new List<UpsertSyncStateRequest>();
        var mailbox = new FakeMailboxStoreClient
        {
            OnGetSyncState = _ => State(Contacts, "65", previousKey: "64", watermark: 11),
            OnUpsertSyncState = req => { writes.Add(req); return new SyncState(); },
        };

        var cache = new FakeSyncRequestCache
        {
            Entry = new CachedSyncRequest { Replayable = false, Collections = [Cached(Contacts, "64")] },
        };

        var controller = CreateController(mailbox, cache, out var body);
        SetBody(controller, null);

        await controller.Post();

        Assert.Equal("13", Value(DecodeResponse(body)!, "Status"));
        Assert.Empty(writes);
    }

    [Fact]
    public async Task A_replay_touches_only_the_cached_collections()
    {
        var read = new List<string>();
        var mailbox = new FakeMailboxStoreClient
        {
            OnGetFolder = req => new Folder { Id = req.ServerId },
            OnGetSyncState = req => { read.Add(req.CollectionId); return State(req.CollectionId, "5"); },
            OnUpsertSyncState = _ => new SyncState(),
            OnGetItemEvents = _ => [],
        };

        var cache = new FakeSyncRequestCache
        {
            Entry = new CachedSyncRequest
            {
                Replayable = true,
                Collections = [Cached(Inbox, "5"), Cached(Contacts, "5")],
            },
        };

        var controller = CreateController(mailbox, cache, out var body);
        SetBody(controller, null);

        await controller.Post();

        Assert.Equal([Inbox, Contacts], read.Distinct());
        Assert.DoesNotContain(Forgotten, read);

        // nothing moved, so MS-ASCMD 2.2.1.21.2 wants headers and no XML payload
        Assert.Null(DecodeResponse(body));
    }

    [Fact]
    public async Task A_replay_that_finds_changes_reports_only_the_cached_collections()
    {
        var mailbox = new FakeMailboxStoreClient
        {
            OnGetFolder = req => new Folder { Id = req.ServerId },
            OnGetSyncState = req => State(req.CollectionId, "5"),
            OnUpsertSyncState = _ => new SyncState(),
            OnGetItemEvents = req => req.CollectionId == Contacts
                ? [new ItemEvent { Id = 1, CommitId = 1, ServerId = "gone", EventType = ChangeEventType.Delete }]
                : [],
        };

        var cache = new FakeSyncRequestCache
        {
            Entry = new CachedSyncRequest
            {
                Replayable = true,
                Collections = [Cached(Inbox, "5"), Cached(Contacts, "5")],
            },
        };

        var controller = CreateController(mailbox, cache, out var body);
        SetBody(controller, null);

        await controller.Post();

        var response = DecodeResponse(body);
        Assert.NotNull(response);

        var collections = Children(response!, "Collections").Single();
        var ids = Children(collections, "Collection").Select(c => Value(c, "CollectionId")).ToList();
        Assert.Equal([Inbox, Contacts], ids);

        var contacts = Children(collections, "Collection").Single(c => Value(c, "CollectionId") == Contacts);
        Assert.Equal("6", Value(contacts, "SyncKey"));
    }

    [Fact]
    public async Task A_replay_that_produced_changes_disarms_the_cache()
    {
        var mailbox = new FakeMailboxStoreClient
        {
            OnGetFolder = req => new Folder { Id = req.ServerId },
            OnGetSyncState = req => State(req.CollectionId, "5"),
            OnUpsertSyncState = _ => new SyncState(),
            OnGetItemEvents = _ => [new ItemEvent { Id = 1, CommitId = 1, ServerId = "gone", EventType = ChangeEventType.Delete }],
        };

        var cache = new FakeSyncRequestCache
        {
            Entry = new CachedSyncRequest { Replayable = true, Collections = [Cached(Inbox, "5")] },
        };

        var controller = CreateController(mailbox, cache, out _);
        SetBody(controller, null);

        await controller.Post();

        Assert.False(cache.Entry!.Replayable);
    }

    [Fact]
    public async Task A_quiet_request_arms_the_cache_and_records_what_the_client_asked_for()
    {
        var mailbox = new FakeMailboxStoreClient
        {
            OnGetFolder = req => new Folder { Id = req.ServerId },
            OnGetSyncState = req => State(req.CollectionId, "5"),
            OnUpsertSyncState = _ => new SyncState(),
            OnGetItemEvents = _ => [],
        };

        var cache = new FakeSyncRequestCache();
        var controller = CreateController(mailbox, cache, out var body);
        SetBody(controller, $"""
            <Collections>
              <Collection>
                <SyncKey>5</SyncKey>
                <CollectionId>{Inbox}</CollectionId>
                <GetChanges/>
                <WindowSize>25</WindowSize>
                <Options>
                  <FilterType>3</FilterType>
                  <airsyncbase:BodyPreference>
                    <airsyncbase:Type>2</airsyncbase:Type>
                    <airsyncbase:TruncationSize>5120</airsyncbase:TruncationSize>
                  </airsyncbase:BodyPreference>
                </Options>
              </Collection>
            </Collections>
            """);

        await controller.Post();

        Assert.Null(DecodeResponse(body));

        var entry = cache.Entry;
        Assert.NotNull(entry);
        Assert.True(entry!.Replayable);

        var cached = Assert.Single(entry.Collections);
        Assert.Equal(Inbox, cached.CollectionId);
        Assert.Equal("5", cached.SyncKey);
        Assert.Equal(25, cached.WindowSize);
        Assert.Equal(3, cached.Options?.FilterType);
        Assert.Equal(5120, Assert.Single(cached.Options!.BodyPreference).TruncationSize);
    }

    [Fact]
    public async Task A_request_that_reports_changes_leaves_the_cache_disarmed()
    {
        var mailbox = new FakeMailboxStoreClient
        {
            OnGetFolder = req => new Folder { Id = req.ServerId },
            OnGetSyncState = req => State(req.CollectionId, "5"),
            OnUpsertSyncState = _ => new SyncState(),
            OnGetItemEvents = _ => [new ItemEvent { Id = 1, CommitId = 1, ServerId = "gone", EventType = ChangeEventType.Delete }],
        };

        var cache = new FakeSyncRequestCache();
        var controller = CreateController(mailbox, cache, out _);
        SetBody(controller, $"""
            <Collections>
              <Collection><SyncKey>5</SyncKey><CollectionId>{Inbox}</CollectionId><GetChanges/></Collection>
            </Collections>
            """);

        await controller.Post();

        Assert.False(cache.Entry!.Replayable);
    }

    // MS-ASCMD 2.2.3.131: unnamed collections keep the settings and sync key of the previous request
    [Fact]
    public async Task A_partial_request_merges_the_cached_collections_and_the_live_one_wins()
    {
        var requested = new List<SyncCollection>();
        var mailbox = new FakeMailboxStoreClient
        {
            OnGetFolder = req => new Folder { Id = req.ServerId },
            OnGetSyncState = req => State(req.CollectionId, req.CollectionId == Inbox ? "9" : "5"),
            OnUpsertSyncState = _ => new SyncState(),
            OnGetItemEvents = _ => [],
        };

        var cache = new FakeSyncRequestCache
        {
            Entry = new CachedSyncRequest
            {
                Replayable = true,
                Collections = [Cached(Inbox, "5"), Cached(Contacts, "5")],
            },
        };

        var controller = CreateController(mailbox, cache, out _);
        SetBody(controller, $"""
            <Partial/>
            <Collections>
              <Collection><SyncKey>9</SyncKey><CollectionId>{Inbox}</CollectionId><GetChanges/></Collection>
            </Collections>
            """);

        await controller.Post();

        var merged = cache.Entry!.Collections;
        Assert.Equal(2, merged.Count);
        Assert.Equal("9", merged.Single(c => c.CollectionId == Inbox).SyncKey);
        Assert.Equal("5", merged.Single(c => c.CollectionId == Contacts).SyncKey);
    }

    [Fact]
    public async Task A_collection_that_answers_status_3_is_not_swallowed_by_the_empty_response()
    {
        var mailbox = new FakeMailboxStoreClient
        {
            OnGetFolder = req => new Folder { Id = req.ServerId },
            OnGetSyncState = req => State(req.CollectionId, "9", previousKey: "8"),
            OnUpsertSyncState = _ => new SyncState(),
            OnGetItemEvents = _ => [],
        };

        var controller = CreateController(mailbox, new FakeSyncRequestCache(), out var body);
        SetBody(controller, $"""
            <Collections>
              <Collection><SyncKey>3</SyncKey><CollectionId>{Inbox}</CollectionId><GetChanges/></Collection>
            </Collections>
            """);

        await controller.Post();

        var response = DecodeResponse(body);
        Assert.NotNull(response);

        var collection = Children(Children(response!, "Collections").Single(), "Collection").Single();
        Assert.Equal("3", Value(collection, "Status"));
    }

    // MS-ASCMD 2.2.3.131: a request has to carry at least Partial or Collections
    [Fact]
    public async Task A_body_naming_neither_Partial_nor_Collections_is_a_protocol_error()
    {
        var cache = new FakeSyncRequestCache
        {
            Entry = new CachedSyncRequest { Replayable = true, Collections = [Cached(Inbox, "5")] },
        };

        var controller = CreateController(new FakeMailboxStoreClient(), cache, out var body);
        SetBody(controller, "<Wait>5</Wait>");

        await controller.Post();

        Assert.Equal("4", Value(DecodeResponse(body)!, "Status"));
    }

    [Fact]
    public async Task A_partial_request_with_nothing_to_merge_is_status_13()
    {
        var controller = CreateController(new FakeMailboxStoreClient(), new FakeSyncRequestCache(), out var body);
        SetBody(controller, "<Partial/>");

        await controller.Post();

        Assert.Equal("13", Value(DecodeResponse(body)!, "Status"));
    }

    [Fact]
    public async Task An_unreadable_body_is_a_protocol_error_not_a_replay()
    {
        var cache = new FakeSyncRequestCache
        {
            Entry = new CachedSyncRequest { Replayable = true, Collections = [Cached(Inbox, "5")] },
        };

        var controller = CreateController(new FakeMailboxStoreClient(), cache, out var body);
        SetBody(controller, null, decodeFailed: true);

        await controller.Post();

        Assert.Equal("4", Value(DecodeResponse(body)!, "Status"));
    }
}
