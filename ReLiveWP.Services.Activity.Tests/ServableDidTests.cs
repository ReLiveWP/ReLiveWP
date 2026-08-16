using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Activity.Tests;

// the provider would hand any of these to anyone, but proxying an arbitrary identity through our
// own server makes us a mirror for accounts the viewer has no relationship with
public class ServableDidTests
{
    private const string Viewer = "user-a";
    private const string Subject = "user-b";
    private const string Provider = "atproto";
    private const string Did = "did:plc:amyamyamyamyamyamyamy";
    private const string StrangerDid = "did:plc:strangerstrangerstra";
    private const long Cid = 0x15fe5d7a6d8d65ff;

    private readonly FakeMailboxStoreClient mailbox = new();
    private readonly FakeConnectedServicesClient connectedServices = new();

    [Fact]
    public async Task A_did_on_the_viewers_own_connection_is_servable()
    {
        connectedServices.OnGetConnections = _ => [new Connection { Service = "atproto", UserId = Did }];

        Assert.True(await NewService().IsServableIdentityAsync(Provider, Did, subjectCid: null, Viewer));
    }

    [Fact]
    public async Task A_did_the_viewer_linked_to_a_contact_is_servable()
    {
        connectedServices.OnGetConnections = _ => [];
        mailbox.OnResolveAuthorsToContacts = _ => new ResolveAuthorsToContactsResponse { ContactCids = { [Did] = Cid } };

        Assert.True(await NewService().IsServableIdentityAsync(Provider, Did, subjectCid: null, Viewer));
    }

    [Fact]
    public async Task A_did_shared_by_a_discoverable_contact_is_servable()
    {
        connectedServices.OnGetConnections = _ => [];
        mailbox.OnResolveFeedSubjects = _ => LiveUser();
        connectedServices.OnGetSharedConnections = _ => new SharedConnectionsResponse
        {
            Connections = { new SharedConnection { OwnerUserId = Subject, Service = "atproto", UserId = Did } },
        };

        Assert.True(await NewService().IsServableIdentityAsync(Provider, Did, Cid, Viewer));
    }

    // the same contact, after they stop sharing or stop being discoverable
    [Fact]
    public async Task A_did_stops_being_servable_when_the_contact_stops_sharing_it()
    {
        connectedServices.OnGetConnections = _ => [];
        mailbox.OnResolveFeedSubjects = _ => LiveUser();
        connectedServices.OnGetSharedConnections = _ => new SharedConnectionsResponse();

        Assert.False(await NewService().IsServableIdentityAsync(Provider, Did, Cid, Viewer));
    }

    [Fact]
    public async Task A_did_stops_being_servable_when_the_contact_stops_being_discoverable()
    {
        connectedServices.OnGetConnections = _ => [];
        mailbox.OnResolveFeedSubjects = _ => new ResolveFeedSubjectsResponse
        {
            Subjects = { new FeedSubject { Cid = Cid, Kind = FeedSubjectKind.Unknown } },
        };

        Assert.False(await NewService().IsServableIdentityAsync(Provider, Did, Cid, Viewer));
    }

    [Fact]
    public async Task An_unrelated_did_is_not_servable()
    {
        connectedServices.OnGetConnections = _ => [new Connection { Service = "atproto", UserId = Did }];
        mailbox.OnResolveFeedSubjects = _ => LiveUser();
        connectedServices.OnGetSharedConnections = _ => new SharedConnectionsResponse
        {
            Connections = { new SharedConnection { OwnerUserId = Subject, Service = "atproto", UserId = Did } },
        };

        Assert.False(await NewService().IsServableIdentityAsync(Provider, StrangerDid, Cid, Viewer));
    }

    [Fact]
    public void A_photo_ref_cannot_steer_the_cdn_path()
    {
        var albums = NewAlbums();

        Assert.False(albums.TryResolvePhoto($"atproto+{Did}+../../evil", out _, out _, out _));
        Assert.False(albums.TryResolvePhoto($"atproto+{Did}+a/b", out _, out _, out _));
        Assert.False(albums.TryResolvePhoto($"atproto+{Did}+a?x=1", out _, out _, out _));
        Assert.False(albums.TryResolvePhoto("atproto+not-a-did+bafyabc123", out _, out _, out _));

        Assert.True(albums.TryResolvePhoto($"atproto+{Did}+bafyabc123", out var provider, out var did, out var cid));
        Assert.Equal(Provider, provider.Provider);
        Assert.Equal(Did, did);
        Assert.Equal("bafyabc123", cid);
    }

    [Fact]
    public void An_album_id_must_name_a_real_did()
    {
        var albums = NewAlbums();

        Assert.False(albums.TryResolveAlbum("atproto+../../evil", out _, out _));
        Assert.True(albums.TryResolveAlbum($"atproto+{Did}", out var provider, out var did));
        Assert.Equal(Provider, provider.Provider);
        Assert.Equal(Did, did);
    }

    // a ref naming a provider we do not have must not fall through to whichever one is registered
    [Fact]
    public void An_unknown_provider_resolves_to_nothing()
    {
        var albums = NewAlbums();

        Assert.False(albums.TryResolveAlbum($"nostr+{Did}", out _, out _));
        Assert.False(albums.TryResolvePhoto($"nostr+{Did}+bafyabc123", out _, out _, out _));
        Assert.False(albums.TryResolveAlbum(Did, out _, out _));
    }

    private static SocialAlbums NewAlbums() =>
        new([new BlueskyAlbumProvider(null!, TestCache.New(), NullLoggerFactory.Instance)]);

    private static ResolveFeedSubjectsResponse LiveUser() =>
        new() { Subjects = { new FeedSubject { Cid = Cid, Kind = FeedSubjectKind.LiveUser, SubjectUserId = Subject } } };

    private ActivityProviderService NewService() =>
        new(null!, null!, NullLoggerFactory.Instance, connectedServices, mailbox, TestCache.New(),
            NullLogger<ActivityProviderService>.Instance);
}
