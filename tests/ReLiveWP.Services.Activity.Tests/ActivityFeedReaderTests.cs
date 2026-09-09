using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Identity;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Activity.Providers;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Activity.Tests;

public class ActivityFeedReaderTests
{
    private const string Viewer = "user-a";
    private const long Cid = 0x15fe5d7a6d8d65ff;
    private const long BoundCid = 0x0123456789abcdef;

    private readonly FakeMailboxStoreClient mailbox = new();

    [Fact]
    public async Task A_contact_feed_is_newest_first_and_capped_to_count()
    {
        var provider = new FakeProvider("atproto",
            Post("old", "did:plc:amy", Day(1)),
            Post("new", "did:plc:amy", Day(3)),
            Post("mid", "did:plc:amy", Day(2)));

        var entries = await NewReader().ReadContactFeedAsync(
            [provider], [new ContactFeedSource("atproto", "did:plc:amy")], Cid, count: 2);

        Assert.Equal(["new", "mid"], entries.Select(e => e.Entry.Id));
        Assert.All(entries, e => Assert.Equal(Cid, e.AuthorCid));
    }

    [Fact]
    public async Task No_sources_means_no_provider_is_asked()
    {
        var provider = new FakeProvider("atproto", Post("a", "did:plc:amy", Day(1)));

        var entries = await NewReader().ReadContactFeedAsync([provider], [], Cid, count: 10);

        Assert.Empty(entries);
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task Bound_authors_get_their_contact_cid_and_the_rest_are_synthesised()
    {
        mailbox.OnResolveAuthorsToContacts = request =>
        {
            Assert.Equal(Viewer, request.UserId);
            Assert.Equal("atproto", request.Provider);
            return new ResolveAuthorsToContactsResponse { ContactCids = { { "did:plc:amy", BoundCid } } };
        };

        var provider = new FakeProvider("atproto",
            Post("a", "did:plc:amy", Day(1)),
            Post("b", "did:plc:bob", Day(2)),
            Post("c", "did:plc:me", Day(3), isMe: true));

        var entries = await NewReader().ReadRepliesAsync([provider], "AT", "root", count: 10, Viewer);

        var byId = entries.ToDictionary(e => e.Entry.Id, e => e.AuthorCid);
        Assert.Equal(BoundCid, byId["a"]);
        Assert.Equal(Cids.SynthesiseAuthorCid("atproto", "did:plc:bob"), byId["b"]);
        Assert.Null(byId["c"]);
    }

    private ActivityFeedReader NewReader() =>
        new(mailbox, NullLogger<ActivityFeedReader>.Instance);

    private static DateTimeOffset Day(int day) => new(2026, 9, day, 0, 0, 0, TimeSpan.Zero);

    private static EntryModel Post(string id, string did, DateTimeOffset published, bool isMe = false) => new()
    {
        Id = id,
        ProviderId = "AT",
        EntryType = EntryType.Post,
        Author = new ProfileModel
        {
            IsMe = isMe,
            Provider = "atproto",
            Id = did,
            DisplayName = did,
            ScreenName = "@" + did,
            AvatarUrl = "",
            CanonicalUrl = "",
        },
        Published = published,
        Title = "Post",
        Content = id,
        Generator = "test",
        CanonicalUrl = "",
        Categories = [],
    };

    private sealed class FakeProvider(string identityProvider, params EntryModel[] entries) : PublicActivityProviderBase
    {
        public int Calls { get; private set; }

        public override string Name => "fake";
        public override string ProviderId => "FAKE";
        public override string IdentityProvider => identityProvider;

        public override async IAsyncEnumerable<EntryModel> GetAuthorEntriesAsync(string provider, string externalId, int count)
        {
            Calls++;
            await Task.Yield();

            if (provider != identityProvider)
                yield break;

            foreach (var entry in entries.Where(e => e.Author.Id == externalId))
                yield return entry;
        }

        public override async IAsyncEnumerable<EntryModel> GetRepliesAsync(string provider, string activityId, int count)
        {
            await Task.Yield();

            foreach (var entry in entries)
                yield return entry;
        }

        public override Task<ResolvedIdentity?> ResolveIdentityAsync(string handleOrId, CancellationToken ct = default)
            => Task.FromResult<ResolvedIdentity?>(null);
    }
}
