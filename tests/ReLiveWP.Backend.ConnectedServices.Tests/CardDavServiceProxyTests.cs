using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Providers;
using ReLiveWP.Backend.ConnectedServices.Proxy;
using ReLiveWP.Backend.ConnectedServices.Services;

namespace ReLiveWP.Backend.ConnectedServices.Tests;

public class CardDavServiceProxyTests
{
    private const string ICloudCollection = "https://p42-contacts.icloud.com/123456789/carddavhome/card/";

    private const string ICloudPhoto = "https://gateway.icloud.com/contacts/123456789/ck/card/abcdefghijklmnopqrstuvwxyz";

    private readonly CardDavServiceProxy proxy;

    public CardDavServiceProxyTests()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IHttpClientFactory>(new FakeHttpClientFactory(new FakeFediverseHandler()))
            .AddSingleton(new ConnectionSecretProtector(new ConfigurationBuilder().Build()))
            .BuildServiceProvider();

        proxy = new CardDavServiceProxy(services);
    }

    [Fact]
    public void Relative_paths_resolve_against_the_collection()
    {
        var target = proxy.GetRequestUrl(Connection(ICloudCollection), new DefaultHttpContext(), "abc.vcf");

        Assert.Equal(ICloudCollection + "abc.vcf", target.ToString());
    }

    [Fact]
    public void An_icloud_account_can_fetch_its_photos_from_the_gateway()
    {
        var target = proxy.GetRequestUrl(Connection(ICloudCollection), new DefaultHttpContext(), ICloudPhoto);

        Assert.Equal(ICloudPhoto, target.ToString());
    }

    [Theory]
    [InlineData("https://dav.fastmail.com/dav/addressbooks/user/me@example.com/Default/")]
    [InlineData("https://contacts.icloud.com.evil.example/123/carddavhome/card/")]
    public void Other_accounts_cannot_reach_the_icloud_gateway(string collection)
    {
        Assert.Throws<InvalidOperationException>(() =>
            proxy.GetRequestUrl(Connection(collection), new DefaultHttpContext(), ICloudPhoto));
    }

    [Theory]
    [InlineData("http://gateway.icloud.com/contacts/123456789/ck/card/abc")]
    [InlineData("https://gateway.icloud.com:8443/contacts/123456789/ck/card/abc")]
    [InlineData("https://gateway.icloud.com.evil.example/contacts/123456789/ck/card/abc")]
    [InlineData("https://www.icloud.com/contacts/123456789/ck/card/abc")]
    [InlineData("https://evil.example/contacts/123456789/ck/card/abc")]
    public void An_icloud_account_reaches_nothing_else_off_host(string url)
    {
        Assert.Throws<InvalidOperationException>(() =>
            proxy.GetRequestUrl(Connection(ICloudCollection), new DefaultHttpContext(), url));
    }

    private static LiveConnectedService Connection(string serviceUrl) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Service = CardDav.SERVICE_NAME,
        ServiceUrl = serviceUrl,
        AccessToken = "",
        RefreshToken = "",
        ExpiresAt = DateTimeOffset.MaxValue,
        Flags = LiveConnectedServiceFlags.None,
        AvailableCapabilities = LiveConnectedServiceCapabilities.Contacts,
        EnabledCapabilities = LiveConnectedServiceCapabilities.Contacts,
    };
}
