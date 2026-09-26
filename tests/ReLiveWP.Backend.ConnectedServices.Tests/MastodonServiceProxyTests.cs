using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Providers;
using ReLiveWP.Backend.ConnectedServices.Proxy;
using ReLiveWP.ServiceDefaults.Outbound;

namespace ReLiveWP.Backend.ConnectedServices.Tests;

public class MastodonServiceProxyTests
{
    private readonly FakeHttpClientFactory factory = new(new FakeFediverseHandler());
    private readonly MastodonServiceProxy proxy;

    public MastodonServiceProxyTests()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IHttpClientFactory>(factory)
            .BuildServiceProvider();

        proxy = new MastodonServiceProxy(services);
    }

    [Fact]
    public async Task Proxied_requests_go_through_the_guarded_client()
    {
        using var client = await proxy.CreateHttpClientAsync(Connection());

        Assert.Equal([OutboundAddressPolicyExtensions.GuardedClientName], factory.RequestedNames);
    }

    [Fact]
    public async Task The_token_goes_out_as_a_bearer()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://snug.moe/api/v1/timelines/home");
        await proxy.AddHeadersAsync(Connection(), request);

        Assert.Equal("Bearer the-token", request.Headers.Authorization?.ToString());
    }

    [Fact]
    public void Paths_resolve_against_the_instance()
    {
        var target = proxy.GetRequestUrl(Connection(), new DefaultHttpContext(), "api/v1/timelines/home");

        Assert.Equal("https://snug.moe/api/v1/timelines/home", target.ToString());
    }

    [Theory]
    [InlineData("/evil.example/api")]
    [InlineData("/127.0.0.1/api")]
    public void A_protocol_relative_path_is_refused(string path)
    {
        Assert.Throws<InvalidOperationException>(() => proxy.GetRequestUrl(Connection(), new DefaultHttpContext(), path));
    }

    [Theory]
    [InlineData("/evil.example/api")]
    [InlineData("\\evil.example/api")]
    [InlineData("\\\\evil.example/api")]
    [InlineData("/\\evil.example/api")]
    [InlineData("@evil.example/api")]
    [InlineData("%2F%2Fevil.example/api")]
    [InlineData("https://evil.example/api")]
    [InlineData("../../evil.example/api")]
    [InlineData("..%2F..%2Fevil.example")]
    [InlineData(":443@evil.example/api")]
    public void No_path_ever_leaves_the_instance(string path)
    {
        Uri target;
        try
        {
            target = proxy.GetRequestUrl(Connection(), new DefaultHttpContext(), path);
        }
        catch (Exception ex) when (ex is InvalidOperationException or UriFormatException)
        {
            return;
        }

        Assert.Equal("snug.moe", target.Host);
        Assert.Equal("https", target.Scheme);
        Assert.Equal(443, target.Port);
    }

    private static LiveConnectedService Connection() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Service = Mastodon.SERVICE_NAME,
        ServiceUrl = "https://snug.moe/",
        AccessToken = "the-token",
        RefreshToken = "",
        ExpiresAt = DateTimeOffset.MaxValue,
        Flags = LiveConnectedServiceFlags.None,
        AvailableCapabilities = LiveConnectedServiceCapabilities.SocialFeed,
        EnabledCapabilities = LiveConnectedServiceCapabilities.SocialFeed,
    };
}
