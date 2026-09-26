using ReLiveWP.Services.Activity.Providers.Mastodon;

namespace ReLiveWP.Services.Activity.Tests;

public class MastodonPublicProviderTests
{
    private static readonly Uri Actor = new(MastodonFixture.SnugActor);

    private readonly MastodonFixture fixture = new();

    private static string Wam() =>
        MastodonFixture.Account("8zlkadc6ao", "wamwoowam", fqn: "wamwoowam@snug.moe", displayName: "Wam");

    [Fact]
    public async Task An_actor_uri_resolves_to_its_account_through_webfinger_and_lookup()
    {
        fixture.ServeSnugAccount();

        var account = await fixture.Resolver.FindAccountAsync(Actor);

        Assert.NotNull(account);
        Assert.Equal(new Uri("https://snug.moe/"), account.Instance);
        Assert.Equal("wamwoowam@snug.moe", account.AccountAddress);
        Assert.Equal("8zlkadc6ao", account.Account.Id);
    }

    [Fact]
    public async Task Account_lookups_are_cached()
    {
        fixture.ServeSnugAccount();

        await fixture.Resolver.FindAccountAsync(Actor);
        var calls = fixture.Server.Requests.Count;
        await fixture.Resolver.FindAccountAsync(Actor);

        Assert.Equal(calls, fixture.Server.Requests.Count);
    }

    [Fact]
    public async Task Webfinger_naming_a_different_actor_is_refused()
    {
        fixture.ServeWebFingerByActor(MastodonFixture.SnugActor, "acct:Gargron@mastodon.social", selfLink: "https://mastodon.social/users/Gargron");

        Assert.Null(await fixture.Resolver.FindAccountAsync(Actor));
        Assert.DoesNotContain(fixture.Server.Requests, r => r.Uri.AbsolutePath == "/api/v1/accounts/lookup");
    }

    [Fact]
    public async Task A_lookup_that_answers_with_a_different_uri_is_refused()
    {
        fixture.ServeWebFingerByActor(MastodonFixture.SnugActor, "acct:wamwoowam@snug.moe");
        fixture.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/accounts/lookup?acct=wamwoowam%40snug.moe",
            MastodonFixture.Account("1", "wamwoowam", uri: "https://snug.moe/users/someone-else"));

        Assert.Null(await fixture.Resolver.FindAccountAsync(Actor));
    }

    [Theory]
    [InlineData("http://snug.moe/users/wam")]
    [InlineData("https://127.0.0.1/users/wam")]
    [InlineData("https://snug.moe:8443/users/wam")]
    [InlineData("https://printer.local/users/wam")]
    public async Task Unsafe_actor_uris_are_never_fetched(string actor)
    {
        Assert.Null(await fixture.Resolver.FindAccountAsync(new Uri(actor)));
        Assert.Empty(fixture.Server.Requests);
    }

    [Fact]
    public async Task An_author_with_a_uri_is_taken_at_its_word_and_one_without_is_webfingered()
    {
        fixture.ServeWebFingerByAcct("snug.moe", "wamwoowam@snug.moe", MastodonFixture.SnugActor);

        var withUri = new MastodonAccount("1", "gargron", "Gargron@mastodon.social", null, null, null, null, "https://mastodon.social/users/Gargron");
        var withoutUri = new MastodonAccount("2", "wamwoowam", "wamwoowam", "wamwoowam@snug.moe", null, null, null, null);
        var unsafeUri = new MastodonAccount("3", "evil", "evil", null, null, null, null, "http://10.0.0.1/users/evil");

        var authors = await fixture.Resolver.ResolveAuthorsAsync([withUri, withoutUri, unsafeUri], new Uri("https://snug.moe/"));

        Assert.Equal("https://mastodon.social/users/Gargron", authors["1"].AbsoluteUri);
        Assert.Equal(MastodonFixture.SnugActor, authors["2"].AbsoluteUri);
        Assert.False(authors.ContainsKey("3"));
    }

    [Fact]
    public async Task The_author_feed_only_carries_that_authors_own_posts()
    {
        fixture.ServeSnugAccount();

        var impostor = MastodonFixture.Account("other", "someone");
        var boosted = MastodonFixture.Status("orig", impostor);
        fixture.Server.OnJson(HttpMethod.Get,
            "https://snug.moe/api/v1/accounts/8zlkadc6ao/statuses?limit=40&exclude_replies=true&exclude_reblogs=true",
            $"[{MastodonFixture.Status("s1", Wam())},{MastodonFixture.Status("s2", impostor)},{MastodonFixture.Status("s3", Wam(), reblog: boosted)},{MastodonFixture.Status("s4", Wam(), visibility: "private")}]");

        var entries = fixture.PublicProvider.GetAuthorEntriesAsync("mastodon", MastodonFixture.SnugActor, 10).ToBlockingEnumerable().ToList();

        var entry = Assert.Single(entries);
        Assert.Equal("snug.moe+s1", entry.Id);
        Assert.Equal(MastodonFixture.SnugActor, entry.Author.Id);
    }

    [Fact]
    public void The_public_provider_ignores_identities_it_does_not_own()
    {
        Assert.Empty(fixture.PublicProvider.GetAuthorEntriesAsync("atproto", MastodonFixture.SnugActor, 10).ToBlockingEnumerable());
        Assert.Empty(fixture.PublicProvider.GetAuthorEntriesAsync("mastodon", "not a uri", 10).ToBlockingEnumerable());
        Assert.Empty(fixture.Server.Requests);
    }

    [Fact]
    public void The_public_provider_carries_no_credentials()
    {
        fixture.ServeSnugAccount();
        fixture.PublicProvider.GetAuthorEntriesAsync("mastodon", MastodonFixture.SnugActor, 10).ToBlockingEnumerable().ToList();

        Assert.NotEmpty(fixture.Server.Requests);
        Assert.All(fixture.Server.Requests, r => Assert.False(r.Headers.ContainsKey("Authorization")));
        Assert.All(fixture.HttpClientFactory.RequestedNames, name => Assert.Equal("GuardedOutbound", name));
    }

    [Theory]
    [InlineData("@wamwoowam@snug.moe")]
    [InlineData("wamwoowam@snug.moe")]
    [InlineData(MastodonFixture.SnugActor)]
    public async Task Identities_resolve_from_a_handle_or_the_actor_uri(string handleOrId)
    {
        fixture.ServeSnugAccount();
        fixture.ServeWebFingerByAcct("snug.moe", "wamwoowam@snug.moe", MastodonFixture.SnugActor);

        var identity = await fixture.PublicProvider.ResolveIdentityAsync(handleOrId);

        Assert.NotNull(identity);
        Assert.Equal("mastodon", identity.Provider);
        Assert.Equal(MastodonFixture.SnugActor, identity.ExternalId);
        Assert.Equal("wamwoowam@snug.moe", identity.Handle);
        Assert.Equal("Wam", identity.DisplayName);
    }

    [Theory]
    [InlineData("wam@127.0.0.1")]
    [InlineData("https://snug.moe/")]
    [InlineData("not a handle")]
    public async Task Unresolvable_identities_come_back_empty(string handleOrId)
    {
        Assert.Null(await fixture.PublicProvider.ResolveIdentityAsync(handleOrId));
    }

    [Fact]
    public void Replies_are_the_direct_children_of_the_post()
    {
        fixture.ServeWebFingerByAcct("snug.moe", "wamwoowam@snug.moe", MastodonFixture.SnugActor);
        var gargron = MastodonFixture.Account("g", "Gargron", uri: "https://mastodon.social/users/Gargron", acct: "Gargron@mastodon.social");

        fixture.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/statuses/s1/context", $$"""
            {"ancestors":[],"descendants":[
              {{MastodonFixture.Status("r1", gargron, inReplyTo: "s1")}},
              {{MastodonFixture.Status("r2", Wam(), inReplyTo: "r1")}},
              {{MastodonFixture.Status("r3", Wam(), inReplyTo: "s1", visibility: "direct")}},
              {{MastodonFixture.Status("r4", Wam(), inReplyTo: "s1")}}]}
            """);

        var replies = fixture.PublicProvider.GetRepliesAsync("MA", "snug.moe+s1", 10).ToBlockingEnumerable().ToList();

        Assert.Equal(["snug.moe+r1", "snug.moe+r4"], replies.Select(r => r.Id));
        Assert.Equal("https://mastodon.social/users/Gargron", replies[0].Author.Id);
        Assert.Equal(MastodonFixture.SnugActor, replies[1].Author.Id);
    }

    [Fact]
    public void Replies_for_an_unsafe_activity_id_are_never_fetched()
    {
        Assert.Empty(fixture.PublicProvider.GetRepliesAsync("MA", "10.0.0.1+s1", 10).ToBlockingEnumerable());
        Assert.Empty(fixture.Server.Requests);
    }
}
