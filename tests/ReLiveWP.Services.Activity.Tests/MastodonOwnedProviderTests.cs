using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Activity.Providers;
using ReLiveWP.Services.Activity.Providers.Mastodon;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Tests;

public class MastodonOwnedProviderTests
{
    private const string Proxy = MastodonFixture.ProxyBase;

    private readonly MastodonFixture fixture = new();

    private static string Wam() =>
        MastodonFixture.Account("8zlkadc6ao", "wamwoowam", fqn: "wamwoowam@snug.moe", displayName: "Wam");

    // truly a diabolical function
    private static string Gargron() =>
        MastodonFixture.Account("g", "Gargron", uri: "https://mastodon.social/users/Gargron", acct: "Gargron@mastodon.social");

    private MastodonActivityProvider NewProvider(string serviceUrl = "https://snug.moe/")
    {
        var connection = new Connection
        {
            Id = "conn-1",
            Service = "mastodon",
            UserId = MastodonFixture.SnugActor,
            ServiceUrl = serviceUrl,
        };

        var proxy = new HttpClient(fixture.Server, disposeHandler: false) { BaseAddress = new Uri(Proxy) };
        return new MastodonActivityProvider(connection, proxy, fixture.Resolver, fixture.PublicProvider, TestCache.New(),
            NullLogger<MastodonActivityProvider>.Instance);
    }

    private void ServeSelf() =>
        fixture.Server.OnJson(HttpMethod.Get, $"{Proxy}api/v1/accounts/verify_credentials", Wam());

    [Fact]
    public void The_home_timeline_pages_until_it_has_enough_and_skips_boosts_and_replies()
    {
        fixture.ServeWebFingerByAcct("snug.moe", "wamwoowam@snug.moe", MastodonFixture.SnugActor);

        fixture.Server.OnJson(HttpMethod.Get, $"{Proxy}api/v1/timelines/home?limit=40",
            $"[{MastodonFixture.Status("h3", Gargron())},{MastodonFixture.Status("h2", Wam(), reblog: MastodonFixture.Status("x", Gargron()))},{MastodonFixture.Status("h1", Wam(), inReplyTo: "h0")}]");
        fixture.Server.OnJson(HttpMethod.Get, $"{Proxy}api/v1/timelines/home?limit=40&max_id=h1",
            $"[{MastodonFixture.Status("g2", Wam())},{MastodonFixture.Status("g1", Gargron())}]");

        var entries = NewProvider().GetEntriesAsync(ActivitiesContext.Contacts, 2).ToBlockingEnumerable().ToList();

        Assert.Equal(["snug.moe+h3", "snug.moe+g2"], entries.Select(e => e.Id));
        Assert.Equal("https://mastodon.social/users/Gargron", entries[0].Author.Id);
        Assert.True(entries[1].Author.IsMe);
    }

    [Fact]
    public void Media_keeps_only_posts_with_photos()
    {
        const string photo = """[{"id":"m1","type":"image","url":"https://snug.moe/files/a.jpg","preview_url":"https://snug.moe/files/a_t.jpg"}]""";
        fixture.Server.OnJson(HttpMethod.Get, $"{Proxy}api/v1/timelines/home?limit=40",
            $"[{MastodonFixture.Status("p1", Gargron())},{MastodonFixture.Status("p2", Gargron(), media: photo)}]");

        var entries = NewProvider().GetEntriesAsync(ActivitiesContext.Media, 10).ToBlockingEnumerable().ToList();

        Assert.Equal("snug.moe+p2", Assert.Single(entries).Id);
    }

    [Fact]
    public void My_reads_the_signed_in_accounts_own_posts()
    {
        ServeSelf();
        fixture.ServeWebFingerByAcct("snug.moe", "wamwoowam@snug.moe", MastodonFixture.SnugActor);
        fixture.Server.OnJson(HttpMethod.Get, $"{Proxy}api/v1/accounts/8zlkadc6ao/statuses?limit=40&exclude_replies=true&exclude_reblogs=true",
            $"[{MastodonFixture.Status("m1", Wam())}]");

        var entries = NewProvider().GetEntriesAsync(ActivitiesContext.My, 10).ToBlockingEnumerable().ToList();

        Assert.Equal("snug.moe+m1", Assert.Single(entries).Id);
    }

    [Fact]
    public async Task A_reply_on_the_home_instance_mentions_the_author_and_keeps_the_visibility()
    {
        ServeSelf();
        fixture.Server.OnJson(HttpMethod.Get, $"{Proxy}api/v1/statuses/s1", MastodonFixture.Status("s1", Gargron(), visibility: "unlisted"));
        fixture.Server.OnJson(HttpMethod.Post, $"{Proxy}api/v1/statuses", MastodonFixture.Status("new", Wam()));

        Assert.True(await NewProvider().CreateReplyAsync("MA", "snug.moe+s1", "nice"));

        var post = fixture.Server.Requests.Single(r => r.Method == HttpMethod.Post);
        using var body = JsonDocument.Parse(post.Body!);

        Assert.Equal("@Gargron@mastodon.social nice", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("s1", body.RootElement.GetProperty("in_reply_to_id").GetString());
        Assert.Equal("unlisted", body.RootElement.GetProperty("visibility").GetString());
        Assert.True(post.Headers.ContainsKey("Idempotency-Key"));
    }

    [Fact]
    public async Task Replying_to_yourself_adds_no_mention_and_public_leaves_visibility_to_the_account_default()
    {
        ServeSelf();
        fixture.Server.OnJson(HttpMethod.Get, $"{Proxy}api/v1/statuses/s1", MastodonFixture.Status("s1", Wam()));
        fixture.Server.OnJson(HttpMethod.Post, $"{Proxy}api/v1/statuses", MastodonFixture.Status("new", Wam()));

        Assert.True(await NewProvider().CreateReplyAsync("MA", "snug.moe+s1", "and another thing"));

        using var body = JsonDocument.Parse(fixture.Server.Requests.Single(r => r.Method == HttpMethod.Post).Body!);
        Assert.Equal("and another thing", body.RootElement.GetProperty("status").GetString());
        Assert.False(body.RootElement.TryGetProperty("visibility", out _));
    }

    [Fact]
    public async Task A_reply_to_a_remote_post_finds_it_on_the_home_instance_first()
    {
        ServeSelf();
        fixture.Server.OnJson(HttpMethod.Get, "https://mastodon.social/api/v1/statuses/111",
            """{"id":"111","uri":"https://mastodon.social/users/Gargron/statuses/111","account":{"id":"1","username":"Gargron"}}""");
        fixture.Server.OnJson(HttpMethod.Get,
            $"{Proxy}api/v2/search?q={Uri.EscapeDataString("https://mastodon.social/users/Gargron/statuses/111")}&type=statuses&resolve=true&limit=1",
            $$"""{"statuses":[{"id":"local-9","uri":"https://mastodon.social/users/Gargron/statuses/111","visibility":"public","account":{{Gargron()}}}]}""");
        fixture.Server.OnJson(HttpMethod.Post, $"{Proxy}api/v1/statuses", MastodonFixture.Status("new", Wam()));

        Assert.True(await NewProvider().CreateReplyAsync("MA", "mastodon.social+111", "hi"));

        using var body = JsonDocument.Parse(fixture.Server.Requests.Single(r => r.Method == HttpMethod.Post).Body!);
        Assert.Equal("local-9", body.RootElement.GetProperty("in_reply_to_id").GetString());
    }

    [Fact]
    public async Task A_search_hit_for_some_other_post_is_not_replied_to()
    {
        ServeSelf();
        fixture.Server.OnJson(HttpMethod.Get, "https://mastodon.social/api/v1/statuses/111",
            """{"id":"111","uri":"https://mastodon.social/users/Gargron/statuses/111","account":{"id":"1","username":"Gargron"}}""");
        fixture.Server.OnJson(HttpMethod.Get,
            $"{Proxy}api/v2/search?q={Uri.EscapeDataString("https://mastodon.social/users/Gargron/statuses/111")}&type=statuses&resolve=true&limit=1",
            $$"""{"statuses":[{"id":"local-9","uri":"https://mastodon.social/users/Gargron/statuses/999","account":{{Gargron()}}}]}""");

        Assert.False(await NewProvider().CreateReplyAsync("MA", "mastodon.social+111", "hi"));
        Assert.DoesNotContain(fixture.Server.Requests, r => r.Method == HttpMethod.Post);
    }

    [Fact]
    public async Task Direct_messages_are_not_replied_into()
    {
        ServeSelf();
        fixture.Server.OnJson(HttpMethod.Get, $"{Proxy}api/v1/statuses/s1", MastodonFixture.Status("s1", Gargron(), visibility: "direct"));

        Assert.False(await NewProvider().CreateReplyAsync("MA", "snug.moe+s1", "hi"));
    }

    [Fact]
    public async Task Other_networks_activity_ids_are_left_alone()
    {
        Assert.False(await NewProvider().CreateReplyAsync("AT", "did:plc:x+app.bsky.feed.post+abc", "hi"));
        Assert.Empty(fixture.Server.Requests);
    }

    [Fact]
    public async Task A_refused_post_throws()
    {
        fixture.Server.OnJson(HttpMethod.Post, $"{Proxy}api/v1/statuses", """{"error":"nope"}""", HttpStatusCode.UnprocessableEntity);

        await Assert.ThrowsAsync<HttpRequestException>(() => NewProvider().CreatePostAsync("hello"));
    }

    [Fact]
    public void Replies_on_another_instance_are_read_publicly_not_through_the_proxy()
    {
        fixture.Server.OnJson(HttpMethod.Get, "https://mastodon.social/api/v1/statuses/111/context", """{"ancestors":[],"descendants":[]}""");

        NewProvider().GetRepliesAsync("MA", "mastodon.social+111", 10).ToBlockingEnumerable().ToList();

        Assert.DoesNotContain(fixture.Server.Requests, r => r.Uri.AbsoluteUri.StartsWith(Proxy));
        Assert.Contains(fixture.Server.Requests, r => r.Uri.AbsoluteUri == "https://mastodon.social/api/v1/statuses/111/context");
    }

    [Theory]
    [InlineData("http://snug.moe/")]
    [InlineData("https://10.0.0.1/")]
    [InlineData("")]
    public async Task A_connection_without_a_usable_instance_does_nothing(string serviceUrl)
    {
        var provider = NewProvider(serviceUrl);

        Assert.Empty(provider.GetEntriesAsync(ActivitiesContext.Contacts, 10).ToBlockingEnumerable());
        Assert.False(await provider.CreateReplyAsync("MA", "snug.moe+s1", "hi"));
        Assert.Empty(fixture.Server.Requests);
    }

    [Fact]
    public async Task Replies_fall_back_to_public_providers_for_networks_the_viewer_has_not_linked()
    {
        var connectedServices = new FakeConnectedServicesClient
        {
            OnGetConnections = _ => [new Connection { Id = "bsky", Service = "atproto", UserId = "did:plc:abc", UserName = "wam.test" }],
        };

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "test")),
        };

        var service = new ActivityProviderService(
            new HttpContextAccessor { HttpContext = context },
            [fixture.PublicProvider],
            [new StubOwnedFactory("atproto")],
            connectedServices, new FakeMailboxStoreClient(), TestCache.New(),
            NullLogger<ActivityProviderService>.Instance);

        var providers = await service.GetReplyProvidersAsync();

        Assert.Contains(providers, p => p is FeedCoalescingActivityProvider);
        Assert.Contains(providers, p => p is PublicMastodonActivityProvider);
    }

    private sealed class StubOwnedFactory(string identityProvider) : IOwnedActivityProviderFactory
    {
        public string IdentityProvider => identityProvider;

        public OwnedActivityProviderBase Create(string userId, Connection connection) => new StubOwnedProvider(identityProvider);
    }

    private sealed class StubOwnedProvider(string identityProvider) : OwnedActivityProviderBase
    {
        public override string Name => identityProvider;
        public override string ProviderId => identityProvider;
        public override string IdentityProvider => identityProvider;

        public override Task CreatePostAsync(string text) => Task.CompletedTask;
        public override Task<bool> CreateReplyAsync(string provider, string activityId, string text) => Task.FromResult(false);
        public override IAsyncEnumerable<EntryModel> GetEntriesAsync(ActivitiesContext context, int count) => AsyncEnumerable.Empty<EntryModel>();
        public override IAsyncEnumerable<EntryModel> GetRepliesAsync(string provider, string activityId, int count) => AsyncEnumerable.Empty<EntryModel>();
    }
}
