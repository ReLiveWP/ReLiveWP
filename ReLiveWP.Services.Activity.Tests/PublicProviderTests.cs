using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Activity.Services;

namespace ReLiveWP.Services.Activity.Tests;

// reading somebody else's posts must never carry a credential, and must never point at the
// connected-service proxy. the type split makes the wrong provider unreachable, this pins the wiring
public class PublicProviderTests
{
    [Fact]
    public void The_public_provider_talks_to_the_appview_and_carries_no_credentials()
    {
        var provider = new PublicBlueskyActivityProvider(NullLoggerFactory.Instance, TestCache.New());
        var protocol = provider.Protocol;

        Assert.Equal("https://public.api.bsky.app/", protocol.Options.Url.ToString());
        Assert.False(protocol.IsAuthenticated);
        Assert.Null(protocol.Session);

        var client = protocol.Client;
        Assert.False(client.DefaultRequestHeaders.Contains("X-User-Id"));
        Assert.False(client.DefaultRequestHeaders.Contains("X-Connection-Id"));
        Assert.Null(client.DefaultRequestHeaders.Authorization);
    }

    [Fact]
    public void The_public_provider_ignores_identities_it_does_not_own()
    {
        var provider = new PublicBlueskyActivityProvider(NullLoggerFactory.Instance, TestCache.New());

        Assert.Empty(provider.GetAuthorEntriesAsync("google", "did:plc:whatever", 10).ToBlockingEnumerable());
    }

    [Fact]
    public void An_unparseable_identity_is_not_fetched()
    {
        var provider = new PublicBlueskyActivityProvider(NullLoggerFactory.Instance, TestCache.New());

        Assert.Empty(provider.GetAuthorEntriesAsync("atproto", "not-a-did", 10).ToBlockingEnumerable());
    }

    // the token-backed provider must not be substitutable where a cross-user read is expected
    [Fact]
    public void The_owned_provider_is_not_a_public_provider()
    {
        Assert.False(typeof(PublicActivityProviderBase).IsAssignableFrom(typeof(BlueskyActivityProvider)));
        Assert.False(typeof(PublicActivityProviderBase).IsAssignableFrom(typeof(FeedCoalescingActivityProvider)));
    }
}
