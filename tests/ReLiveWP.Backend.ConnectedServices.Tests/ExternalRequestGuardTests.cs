using ReLiveWP.Backend.ConnectedServices.Providers;
using ReLiveWP.ServiceDefaults.Outbound;

using ServiceCaps = ReLiveWP.Backend.ConnectedServices.Data.LiveConnectedServiceCapabilities;

namespace ReLiveWP.Backend.ConnectedServices.Tests;

public class ExternalRequestGuardTests
{
    private static readonly Uri Snug = new("https://snug.moe/");

    [Theory]
    [InlineData("https://snug.moe/oauth/token")]
    [InlineData("https://SNUG.moe/api/v1/apps")]
    public void Endpoints_on_the_instance_are_accepted(string uri)
    {
        Assert.True(ExternalRequestGuard.IsOnInstance(new Uri(uri), Snug));
    }

    [Theory]
    [InlineData("https://evil.example/oauth/token")]
    [InlineData("https://snug.moe.evil.example/oauth/token")]
    [InlineData("https://sub.snug.moe/oauth/token")]
    [InlineData("http://snug.moe/oauth/token")]
    [InlineData("https://snug.moe:8443/oauth/token")]
    [InlineData("https://user@snug.moe/oauth/token")]
    [InlineData("https://127.0.0.1/oauth/token")]
    [InlineData("https://[::1]/oauth/token")]
    public void Endpoints_anywhere_else_are_refused(string uri)
    {
        Assert.False(ExternalRequestGuard.IsOnInstance(new Uri(uri), Snug));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/relative/path")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/plain,hi")]
    [InlineData("ftp://snug.moe/")]
    [InlineData("https://localhost/")]
    [InlineData("https://localhost./")]
    [InlineData("https://intranet./")]
    [InlineData("https://snug.moe./")]
    [InlineData("https://169.254.169.254/latest/meta-data/")]
    public void Unusable_uris_do_not_parse(string? value)
    {
        Assert.False(ExternalRequestGuard.TryParseAcceptableUri(value, out _));
    }

    [Fact]
    public void The_guarded_client_is_the_policy_client_with_a_size_cap()
    {
        var factory = new FakeHttpClientFactory(new FakeFediverseHandler());

        using var client = ExternalRequestGuard.CreateGuardedClient(factory);

        Assert.Equal([OutboundAddressPolicyExtensions.GuardedClientName], factory.RequestedNames);
        Assert.Equal(ExternalRequestGuard.MaxResponseBytes, client.MaxResponseContentBufferSize);
    }

    [Fact]
    public void Granular_scopes_map_to_capabilities()
    {
        var caps = MastodonOAuthProvider.GetCapabilitiesFromScopes(Mastodon.REQUESTED_SCOPES.Split(' '));

        Assert.Equal(ServiceCaps.SocialFeed | ServiceCaps.SocialPhotos | ServiceCaps.SocialNotifications | ServiceCaps.SocialPost, caps);
    }

    [Fact]
    public void The_fallback_scopes_map_to_the_same_capabilities()
    {
        var caps = MastodonOAuthProvider.GetCapabilitiesFromScopes(Mastodon.FALLBACK_SCOPES.Split(' '));

        Assert.Equal(ServiceCaps.SocialFeed | ServiceCaps.SocialPhotos | ServiceCaps.SocialNotifications | ServiceCaps.SocialPost, caps);
    }

    [Fact]
    public void A_read_only_grant_cannot_post()
    {
        var caps = MastodonOAuthProvider.GetCapabilitiesFromScopes(["read:accounts", "read:statuses"]);

        Assert.False(caps.HasFlag(ServiceCaps.SocialPost));
        Assert.True(caps.HasFlag(ServiceCaps.SocialFeed));
        Assert.False(caps.HasFlag(ServiceCaps.SocialNotifications));
    }
}
