using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Activity.Providers.Mastodon;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Tests;

public class MastodonNotificationTests
{
    private const string Proxy = MastodonFixture.ProxyBase;

    private readonly MastodonFixture fixture = new();

    private static string Wam() =>
        MastodonFixture.Account("8zlkadc6ao", "wamwoowam", fqn: "wamwoowam@snug.moe", displayName: "Wam");

    private static string Gargron() =>
        MastodonFixture.Account("g", "Gargron", uri: "https://mastodon.social/users/Gargron", acct: "Gargron@mastodon.social",
            displayName: "Eugen");

    private MastodonActivityProvider NewProvider()
    {
        var connection = new Connection
        {
            Id = "conn-1",
            Service = "mastodon",
            UserId = MastodonFixture.SnugActor,
            ServiceUrl = "https://snug.moe/",
        };

        var proxy = new HttpClient(fixture.Server, disposeHandler: false) { BaseAddress = new Uri(Proxy) };
        return new MastodonActivityProvider(connection, proxy, fixture.Resolver, fixture.PublicProvider, TestCache.New(),
            NullLogger<MastodonActivityProvider>.Instance);
    }

    private void ServeNotifications(string body, string? maxId = null)
    {
        var url = maxId == null ? $"{Proxy}api/v1/notifications?limit=40" : $"{Proxy}api/v1/notifications?limit=40&max_id={maxId}";
        fixture.Server.OnJson(HttpMethod.Get, url, body);
    }

    private static string MentionFromGargron(string id, string statusId = "s1", string createdAt = "2026-09-25T12:00:00Z") =>
        MastodonFixture.Notification(id, "mention", Gargron(), MastodonFixture.Status(statusId, Gargron(), "<p>well quite</p>"),
            createdAt);

    [Fact]
    public void A_mention_carries_the_post_that_mentioned_you()
    {
        fixture.ServeWebFingerByActor("https://mastodon.social/users/Gargron", "acct:Gargron@mastodon.social");
        ServeNotifications($"[{MentionFromGargron("n1")}]");

        var entry = Assert.Single(NewProvider().GetNotificationsAsync(10, null).ToBlockingEnumerable());

        Assert.Equal("Eugen mentioned you", entry.Title);
        Assert.Equal("well quite", entry.Content);
        Assert.Equal("snug.moe+n1", entry.Id);
        Assert.Equal("https://snug.moe/notes/s1", entry.CanonicalUrl);
        Assert.False(entry.Author.IsMe);
    }

    // the pivot row says "mentioned you" whatever we send, so anything that isn't one stays off the phone
    [Theory]
    [InlineData("favourite")]
    [InlineData("reblog")]
    [InlineData("follow")]
    [InlineData("status")]
    [InlineData("poll")]
    [InlineData("update")]
    [InlineData("admin.sign_up")]
    public void Only_mentions_reach_the_phone(string type)
    {
        fixture.ServeWebFingerByActor("https://mastodon.social/users/Gargron", "acct:Gargron@mastodon.social");
        ServeNotifications($"""
            [{MastodonFixture.Notification("n2", type, Gargron(), MastodonFixture.Status("s2", Wam(), "<p>mine</p>"))},
             {MentionFromGargron("n1")}]
            """);

        var entry = Assert.Single(NewProvider().GetNotificationsAsync(10, null).ToBlockingEnumerable());

        Assert.Equal("snug.moe+n1", entry.Id);
    }

    // the phone throws away anything not strictly newer than its marker, so there is no point sending it
    [Fact]
    public void Anything_the_phone_has_already_seen_ends_the_walk()
    {
        fixture.ServeWebFingerByActor("https://mastodon.social/users/Gargron", "acct:Gargron@mastodon.social");
        ServeNotifications($"""
            [{MentionFromGargron("n3", "s3", "2026-09-25T12:00:00Z")},
             {MentionFromGargron("n2", "s2", "2026-09-24T12:00:00Z")},
             {MentionFromGargron("n1", "s1", "2026-09-23T12:00:00Z")}]
            """);

        var since = new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);
        var entries = NewProvider().GetNotificationsAsync(10, since).ToBlockingEnumerable().ToList();

        Assert.Equal(["snug.moe+n3"], entries.Select(e => e.Id));
    }

    [Fact]
    public void The_walk_pages_until_it_has_enough()
    {
        fixture.ServeWebFingerByActor("https://mastodon.social/users/Gargron", "acct:Gargron@mastodon.social");
        ServeNotifications($"[{MentionFromGargron("n3", "s3")}]");
        ServeNotifications($"[{MentionFromGargron("n2", "s2")}]", maxId: "n3");

        var entries = NewProvider().GetNotificationsAsync(2, null).ToBlockingEnumerable().ToList();

        Assert.Equal(["snug.moe+n3", "snug.moe+n2"], entries.Select(e => e.Id));
    }

    [Fact]
    public void An_instance_that_refuses_notifications_yields_nothing()
    {
        Assert.Empty(NewProvider().GetNotificationsAsync(10, null).ToBlockingEnumerable());
    }
}
