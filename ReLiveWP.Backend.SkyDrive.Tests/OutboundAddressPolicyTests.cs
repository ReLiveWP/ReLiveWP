using System.Net;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;
using ReLiveWP.Backend.ConnectedServices.Services;

namespace ReLiveWP.Backend.SkyDrive.Tests;

// The WebDAV target address comes from the user, so this predicate is the only thing standing
// between a linked share and the internal network. It has no integration coverage by design.
public class OutboundAddressPolicyTests
{
    private readonly PublicOnlyAddressPolicy policy = new();

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.1.2.3")]
    [InlineData("0.0.0.0")]
    [InlineData("10.0.0.1")]
    [InlineData("10.255.255.255")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("100.127.255.255")]
    [InlineData("192.0.0.1")]
    [InlineData("198.18.0.1")]
    [InlineData("198.19.255.255")]
    [InlineData("224.0.0.1")]
    [InlineData("239.255.255.255")]
    [InlineData("255.255.255.255")]
    public void BlocksNonPublicV4(string address)
        => Assert.False(policy.IsAllowed(IPAddress.Parse(address)));

    [Theory]
    [InlineData("::1")]
    [InlineData("::")]
    [InlineData("fc00::1")]
    [InlineData("fd12:3456::1")]
    [InlineData("fe80::1")]
    [InlineData("ff02::1")]
    [InlineData("2001:db8::1")]
    public void BlocksNonPublicV6(string address)
        => Assert.False(policy.IsAllowed(IPAddress.Parse(address)));

    // an attacker who can't use a literal will reach for the v6 wrapper of the same address
    [Theory]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:169.254.169.254")]
    [InlineData("::ffff:10.0.0.1")]
    [InlineData("::ffff:192.168.0.1")]
    public void BlocksIPv4MappedV6(string address)
        => Assert.False(policy.IsAllowed(IPAddress.Parse(address)));

    [Theory]
    [InlineData("1.1.1.1")]
    [InlineData("8.8.8.8")]
    [InlineData("172.15.255.255")]
    [InlineData("172.32.0.1")]
    [InlineData("100.63.255.255")]
    [InlineData("100.128.0.1")]
    [InlineData("192.0.1.1")]
    [InlineData("198.17.255.255")]
    [InlineData("198.20.0.1")]
    [InlineData("2606:4700::1111")]
    public void AllowsPublicAddresses(string address)
        => Assert.True(policy.IsAllowed(IPAddress.Parse(address)));

    [Theory]
    [InlineData("https")]
    public void AllowsHttpsOnly(string scheme)
        => Assert.True(policy.IsAllowedScheme(scheme));

    [Theory]
    [InlineData("http")]
    [InlineData("ftp")]
    [InlineData("file")]
    [InlineData("gopher")]
    public void RejectsEveryOtherScheme(string scheme)
        => Assert.False(policy.IsAllowedScheme(scheme));

    [Fact]
    public void ValidateUriRejectsPlainHttp()
        => Assert.Throws<CredentialLinkException>(() => policy.ValidateUri(new Uri("http://cloud.example.com/dav/")));

    [Theory]
    [InlineData("https://127.0.0.1/dav/")]
    [InlineData("https://192.168.1.10/dav/")]
    [InlineData("https://[::1]/dav/")]
    [InlineData("https://169.254.169.254/latest/meta-data/")]
    public void ValidateUriRejectsPrivateLiterals(string url)
        => Assert.Throws<CredentialLinkException>(() => policy.ValidateUri(new Uri(url)));

    [Fact]
    public void ValidateUriAcceptsAPublicHttpsHost()
        => policy.ValidateUri(new Uri("https://cloud.example.com/remote.php/dav/files/me/"));

    // a hostname passing here is not a decision, only a deferral: the ConnectCallback re-checks the
    // resolved address, which is what actually defeats rebinding
    [Fact]
    public void ValidateUriDefersHostnamesToConnectTime()
        => policy.ValidateUri(new Uri("https://rebind.example.com/dav/"));

    [Fact]
    public void UnrestrictedPolicyIsOnlyForTestHosts()
    {
        var permissive = new UnrestrictedAddressPolicy();

        Assert.True(permissive.IsAllowed(IPAddress.Loopback));
        Assert.True(permissive.IsAllowedScheme("http"));
    }
}
