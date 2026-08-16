using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Activity.Tests;

public class ContactFeedSourceTests
{
    private const string Viewer = "user-a";
    private const string Subject = "user-b";
    private const string Did = "did:plc:amyamyamyamyamyamyamy";
    private const long Cid = 0x15fe5d7a6d8d65ff;

    private const uint SocialFeed = 0x20;

    private readonly FakeMailboxStoreClient mailbox = new();
    private readonly FakeConnectedServicesClient connectedServices = new();

    [Fact]
    public async Task A_faux_identity_becomes_a_source_without_asking_about_sharing()
    {
        mailbox.OnResolveFeedSubjects = _ => Subjects(new FeedSubject
        {
            Cid = Cid,
            Kind = FeedSubjectKind.ContactIdentity,
            Identities = { new ContactIdentity { Provider = "atproto", ExternalId = Did, ContactCid = Cid } },
        });

        var sources = await NewService().GetContactFeedSourcesAsync(Cid, Viewer);

        var source = Assert.Single(sources);
        Assert.Equal("atproto", source.Provider);
        Assert.Equal(Did, source.ExternalId);
        Assert.Equal(0, connectedServices.GetSharedConnectionsCalls);
    }

    [Fact]
    public async Task A_live_user_who_shares_a_feed_becomes_a_source()
    {
        mailbox.OnResolveFeedSubjects = _ => Subjects(LiveUser());
        connectedServices.OnGetSharedConnections = _ => new SharedConnectionsResponse
        {
            Connections = { new SharedConnection { OwnerUserId = Subject, Service = "atproto", UserId = Did } },
        };

        var sources = await NewService().GetContactFeedSourcesAsync(Cid, Viewer);

        var source = Assert.Single(sources);
        Assert.Equal("atproto", source.Provider);
        Assert.Equal(Did, source.ExternalId);
    }

    [Fact]
    public async Task A_live_user_who_shares_nothing_yields_no_source()
    {
        mailbox.OnResolveFeedSubjects = _ => Subjects(LiveUser());
        connectedServices.OnGetSharedConnections = _ => new SharedConnectionsResponse();

        Assert.Empty(await NewService().GetContactFeedSourcesAsync(Cid, Viewer));
    }

    [Fact]
    public async Task The_lookup_asks_only_for_the_feed_capability()
    {
        SharedConnectionsRequest? seen = null;

        mailbox.OnResolveFeedSubjects = _ => Subjects(LiveUser());
        connectedServices.OnGetSharedConnections = r =>
        {
            seen = r;
            return new SharedConnectionsResponse();
        };

        await NewService().GetContactFeedSourcesAsync(Cid, Viewer);

        Assert.NotNull(seen);
        Assert.Equal([Subject], seen!.UserIds);
        Assert.Equal(SocialFeed, seen.Capabilities);
    }

    [Fact]
    public async Task An_unresolved_cid_yields_no_source()
    {
        mailbox.OnResolveFeedSubjects = _ => Subjects(new FeedSubject { Cid = Cid, Kind = FeedSubjectKind.Unknown });

        Assert.Empty(await NewService().GetContactFeedSourcesAsync(Cid, Viewer));
    }

    // a mailbox that cannot answer must not be read as a yes
    [Fact]
    public async Task A_failed_authorization_yields_no_source()
    {
        mailbox.OnResolveFeedSubjects = _ => throw new InvalidOperationException("mailbox is down");

        Assert.Empty(await NewService().GetContactFeedSourcesAsync(Cid, Viewer));
    }

    // what a subject shares is the same for everyone, so it caches. nothing about who asked goes in
    // the key, and the decision that they may ask is made upstream every time
    [Fact]
    public async Task What_a_subject_shares_is_only_fetched_once()
    {
        mailbox.OnResolveFeedSubjects = _ => Subjects(LiveUser());
        connectedServices.OnGetSharedConnections = _ => new SharedConnectionsResponse
        {
            Connections = { new SharedConnection { OwnerUserId = Subject, Service = "atproto", UserId = Did } },
        };

        var service = NewService();
        await service.GetContactFeedSourcesAsync(Cid, Viewer);
        await service.GetContactFeedSourcesAsync(Cid, "someone-else");

        Assert.Equal(1, connectedServices.GetSharedConnectionsCalls);
    }

    private static FeedSubject LiveUser() =>
        new() { Cid = Cid, Kind = FeedSubjectKind.LiveUser, SubjectUserId = Subject };

    private static ResolveFeedSubjectsResponse Subjects(FeedSubject subject) =>
        new() { Subjects = { subject } };

    private ActivityProviderService NewService() =>
        new(null!, null!, NullLoggerFactory.Instance, connectedServices, mailbox, TestCache.New(),
            NullLogger<ActivityProviderService>.Instance);
}
