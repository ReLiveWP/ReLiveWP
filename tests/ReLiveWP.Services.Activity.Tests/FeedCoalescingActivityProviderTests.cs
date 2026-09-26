using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Activity.Providers;

namespace ReLiveWP.Services.Activity.Tests;

public class FeedCoalescingActivityProviderTests
{
    [Fact]
    public void A_cross_post_shows_once_in_the_feed()
    {
        var bluesky = new FakeOwnedProvider("atproto", entries: [Post("AT", "b1", "same words", Day(2))]);
        var mastodon = new FakeOwnedProvider("mastodon", entries: [Post("MA", "m1", "Same Words", Day(1))]);

        var entries = NewCoalescer(bluesky, mastodon)
            .GetEntriesAsync(ActivitiesContext.Contacts, 10).ToBlockingEnumerable().ToList();

        Assert.Equal("b1", Assert.Single(entries).Id);
    }

    [Fact]
    public void Different_posts_all_survive_the_dedup()
    {
        var bluesky = new FakeOwnedProvider("atproto", entries: [Post("AT", "b1", "one", Day(2))]);
        var mastodon = new FakeOwnedProvider("mastodon", entries: [Post("MA", "m1", "two", Day(1))]);

        var entries = NewCoalescer(bluesky, mastodon)
            .GetEntriesAsync(ActivitiesContext.Contacts, 10).ToBlockingEnumerable().ToList();

        Assert.Equal(["b1", "m1"], entries.Select(e => e.Id));
    }

    [Fact]
    public void A_reply_read_through_two_accounts_shows_once()
    {
        const string url = "https://mastodon.social/@Gargron/111";
        var onSnug = new FakeOwnedProvider("mastodon", replies: [Post("MA", "snug.moe+local-1", "hi", Day(1), url)]);
        var onSocial = new FakeOwnedProvider("mastodon", replies: [Post("MA", "mastodon.social+111", "hi", Day(1), url)]);

        var replies = NewCoalescer(onSnug, onSocial)
            .GetRepliesAsync("MA", "mastodon.social+100", 10).ToBlockingEnumerable().ToList();

        Assert.Equal("snug.moe+local-1", Assert.Single(replies).Id);
    }

    [Fact]
    public void Replies_without_a_url_fall_back_to_their_id()
    {
        var first = new FakeOwnedProvider("atproto", replies: [Post("AT", "r1", "a", Day(1)), Post("AT", "r2", "b", Day(1))]);
        var second = new FakeOwnedProvider("atproto", replies: [Post("AT", "r1", "a", Day(1))]);

        var replies = NewCoalescer(first, second)
            .GetRepliesAsync("AT", "root", 10).ToBlockingEnumerable().ToList();

        Assert.Equal(["r1", "r2"], replies.Select(r => r.Id));
    }

    [Fact]
    public async Task One_network_refusing_a_post_does_not_fail_the_others()
    {
        var bluesky = new FakeOwnedProvider("atproto");
        var mastodon = new FakeOwnedProvider("mastodon", postFailure: new HttpRequestException("nope"));

        await NewCoalescer(bluesky, mastodon).CreatePostAsync("hello");

        Assert.Equal(["hello"], bluesky.Posted);
    }

    [Fact]
    public async Task Every_network_refusing_a_post_throws()
    {
        var bluesky = new FakeOwnedProvider("atproto", postFailure: new HttpRequestException("no"));
        var mastodon = new FakeOwnedProvider("mastodon", postFailure: new HttpRequestException("nope"));

        var thrown = await Assert.ThrowsAsync<AggregateException>(() => NewCoalescer(bluesky, mastodon).CreatePostAsync("hello"));

        Assert.Equal(2, thrown.InnerExceptions.Count);
    }

    [Fact]
    public async Task Posting_with_nothing_linked_does_nothing()
    {
        await NewCoalescer().CreatePostAsync("hello");
    }

    private static FeedCoalescingActivityProvider NewCoalescer(params OwnedActivityProviderBase[] providers) =>
        new(providers, NullLogger.Instance);

    private static DateTimeOffset Day(int day) => new(2026, 9, day, 0, 0, 0, TimeSpan.Zero);

    private static EntryModel Post(string providerId, string id, string content, DateTimeOffset published, string canonicalUrl = "") => new()
    {
        Id = id,
        ProviderId = providerId,
        EntryType = EntryType.Post,
        Author = new ProfileModel
        {
            Id = "author",
            DisplayName = "author",
            ScreenName = "@author",
            AvatarUrl = "",
            CanonicalUrl = "",
        },
        Published = published,
        Title = "Post",
        Content = content,
        Generator = "test",
        CanonicalUrl = canonicalUrl,
        Categories = [],
    };

    private sealed class FakeOwnedProvider(
        string identityProvider,
        EntryModel[]? entries = null,
        EntryModel[]? replies = null,
        Exception? postFailure = null) : OwnedActivityProviderBase
    {
        public List<string> Posted { get; } = [];

        public override string Name => identityProvider;
        public override string ProviderId => identityProvider;
        public override string IdentityProvider => identityProvider;

        public override async Task CreatePostAsync(string text)
        {
            await Task.Yield();

            if (postFailure != null)
                throw postFailure;

            Posted.Add(text);
        }

        public override Task<bool> CreateReplyAsync(string provider, string activityId, string text) => Task.FromResult(false);

        public override IAsyncEnumerable<EntryModel> GetEntriesAsync(ActivitiesContext context, int count)
            => (entries ?? []).ToAsyncEnumerable();

        public override IAsyncEnumerable<EntryModel> GetRepliesAsync(string provider, string activityId, int count)
            => (replies ?? []).ToAsyncEnumerable();
    }
}
