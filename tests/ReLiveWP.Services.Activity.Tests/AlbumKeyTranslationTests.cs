using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Activity.Providers;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Activity.Utilities;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Activity.Tests;

public class AlbumKeyTranslationTests
{
    private const string Viewer = "user-a";
    private const string Subject = "user-b";
    private const long Cid = 0x15fe5d7a6d8d65ff;

    private readonly FakeKeyedAlbumProvider provider = new();
    private readonly FakeConnectedServicesClient connectedServices = new();
    private readonly FakeMailboxStoreClient mailbox = new();

    [Fact]
    public async Task An_album_key_is_servable_when_the_identity_behind_it_is()
    {
        connectedServices.OnGetConnections = _ => [OwnConnection(FakeKeyedAlbumProvider.AmyIdentity)];

        Assert.True(await NewService().CanServeAsync(provider, FakeKeyedAlbumProvider.AmyKey, subjectCid: null, Viewer));
    }

    [Fact]
    public async Task An_album_key_with_no_identity_behind_it_is_not_servable()
    {
        connectedServices.OnGetConnections = _ => [OwnConnection("keyed.example+nobody")];

        Assert.False(await NewService().CanServeAsync(provider, "keyed.example+nobody", subjectCid: null, Viewer));
    }

    [Fact]
    public async Task The_gate_asks_the_mailbox_about_the_identity_not_the_key()
    {
        connectedServices.OnGetConnections = _ => [];

        List<string> asked = [];
        mailbox.OnResolveAuthorsToContacts = request =>
        {
            asked.AddRange(request.ExternalIds);
            return new ResolveAuthorsToContactsResponse { ContactCids = { [FakeKeyedAlbumProvider.AmyIdentity] = Cid } };
        };

        Assert.True(await NewService().CanServeAsync(provider, FakeKeyedAlbumProvider.AmyKey, subjectCid: null, Viewer));
        Assert.Equal([FakeKeyedAlbumProvider.AmyIdentity], asked);
    }

    [Fact]
    public async Task A_shared_source_that_will_not_translate_is_skipped_and_the_rest_are_kept()
    {
        mailbox.OnResolveFeedSubjects = _ => new ResolveFeedSubjectsResponse
        {
            Subjects = { new FeedSubject { Cid = Cid, Kind = FeedSubjectKind.LiveUser, SubjectUserId = Subject } },
        };
        connectedServices.OnGetSharedConnections = _ => new SharedConnectionsResponse
        {
            Connections =
            {
                new SharedConnection { OwnerUserId = Subject, Service = FakeKeyedAlbumProvider.Token, UserId = FakeKeyedAlbumProvider.BenIdentity },
                new SharedConnection { OwnerUserId = Subject, Service = FakeKeyedAlbumProvider.Token, UserId = FakeKeyedAlbumProvider.AmyIdentity },
            },
        };

        var albums = await NewService().SharedAlbumsAsync(Cid, Viewer);

        var album = Assert.Single(albums);
        Assert.Equal(SocialAlbumRef.ForAlbum(FakeKeyedAlbumProvider.Token, FakeKeyedAlbumProvider.AmyKey), album.ResourceId);
        Assert.Equal("@amy@keyed.example's photos", album.Title);
    }

    [Fact]
    public async Task Opening_your_own_album_finds_the_connection_by_identity()
    {
        var connection = OwnConnection(FakeKeyedAlbumProvider.AmyIdentity);
        connectedServices.OnGetConnections = _ => [connection];

        await NewService().FolderAsync(provider, FakeKeyedAlbumProvider.AmyKey, Viewer);

        var passed = Assert.Single(provider.AlbumConnections);
        Assert.NotNull(passed);
        Assert.Equal(connection.Id, passed.Id);
    }

    [Fact]
    public async Task An_album_key_that_happens_to_equal_a_connection_id_is_not_mistaken_for_it()
    {
        connectedServices.OnGetConnections = _ => [OwnConnection(FakeKeyedAlbumProvider.AmyKey)];

        await NewService().FolderAsync(provider, FakeKeyedAlbumProvider.AmyKey, Viewer);

        Assert.Null(Assert.Single(provider.AlbumConnections));
    }

    [Fact]
    public async Task A_photo_that_does_not_resolve_is_left_out_of_the_web_album()
    {
        provider.MediaSource = null;
        var renderer = new AlbumRenderService(null!, new SocialAlbumsService([provider]), TestMediaProxy.CreateSigner());
        var photo = new SocialPhoto(SocialAlbumRef.ForPhoto(FakeKeyedAlbumProvider.Token, FakeKeyedAlbumProvider.AmyKey, "photo1"),
                                    "photo1.jpg", null, DateTime.UnixEpoch, 1, 1);

        var response = await renderer.RenderSocialLibraryAsync(
            SocialAlbumRef.ForAlbum(FakeKeyedAlbumProvider.Token, FakeKeyedAlbumProvider.AmyKey),
            new SocialAlbumFolder("photos", [photo]));
        var summary = await renderer.RenderSocialSummaryAsync(new SocialAlbum("keyed+keyed.example+amy", "photos"), photo.ResourceRef);

        Assert.Empty(response.Photos);
        Assert.Null(summary.CoverUrl);
    }

    private static Connection OwnConnection(string userId)
        => new() { Id = "connection-1", Service = FakeKeyedAlbumProvider.Token, UserId = userId };

    private SocialAlbumService NewService()
    {
        var activity = new ActivityProviderService(null!, [], [], connectedServices, mailbox, TestCache.New(),
                                                   NullLogger<ActivityProviderService>.Instance);
        return new SocialAlbumService(new SocialAlbumsService([provider]), new ConnectionLookupService(connectedServices),
                                      activity, NullLogger<SocialAlbumService>.Instance);
    }
}
