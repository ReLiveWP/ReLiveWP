using ReLiveWP.Backend.ConnectedServices.OAuthProviders;

namespace ReLiveWP.Backend.SkyDrive.Tests;

// A username alone can't distinguish two shares on the same server, so the derived label used when
// nobody names the share has to carry the whole location.
public class WebDavShareLabelTests
{
    private static string Describe(string url, string username)
        => WebDavCredentialProvider.DescribeShare(WebDavCredentialProvider.NormaliseUri(url), username);

    [Theory]
    [InlineData("https://cloud.example.com/remote.php/dav/files/me/", "wam", "wam@cloud.example.com/remote.php/dav/files/me")]
    [InlineData("https://cloud.example.com/", "wam", "wam@cloud.example.com")]
    [InlineData("https://cloud.example.com", "wam", "wam@cloud.example.com")]
    [InlineData("https://nas.example.com/photos", "wam", "wam@nas.example.com/photos")]
    public void DescribesTheWholeShareLocation(string url, string username, string expected)
        => Assert.Equal(expected, Describe(url, username));

    [Fact]
    public void KeepsANonDefaultPort()
        => Assert.Equal("wam@nas.example.com:8443/photos", Describe("https://nas.example.com:8443/photos", "wam"));

    [Fact]
    public void DropsTheDefaultHttpsPort()
        => Assert.Equal("wam@nas.example.com/photos", Describe("https://nas.example.com:443/photos", "wam"));

    // the whole point: two shares on one server, same user, must not read identically
    [Fact]
    public void DistinguishesSharesOnTheSameServer()
    {
        var photos = Describe("https://nas.example.com/photos", "wam");
        var backups = Describe("https://nas.example.com/backups", "wam");

        Assert.NotEqual(photos, backups);
    }

    [Theory]
    [InlineData("cloud.example.com/dav", "https://cloud.example.com/dav/")]
    [InlineData("https://cloud.example.com/dav", "https://cloud.example.com/dav/")]
    [InlineData("https://cloud.example.com/dav/", "https://cloud.example.com/dav/")]
    public void NormalisesToAnHttpsUrlWithATrailingSlash(string input, string expected)
        => Assert.Equal(expected, WebDavCredentialProvider.NormaliseUri(input).ToString());

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url at all::::")]
    public void RejectsUnusableAddresses(string input)
        => Assert.Throws<CredentialLinkException>(() => WebDavCredentialProvider.NormaliseUri(input));

    // credentials belong in their own fields, not smuggled into the address
    [Fact]
    public void RejectsCredentialsEmbeddedInTheUrl()
        => Assert.Throws<CredentialLinkException>(
            () => WebDavCredentialProvider.NormaliseUri("https://wam:hunter2@cloud.example.com/dav/"));
}
