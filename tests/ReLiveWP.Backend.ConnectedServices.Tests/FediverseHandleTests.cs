using ReLiveWP.ServiceDefaults.Outbound;

namespace ReLiveWP.Backend.ConnectedServices.Tests;

public class FediverseHandleTests
{
    [Theory]
    [InlineData("@wamwoowam@snug.moe", "wamwoowam", "snug.moe")]
    [InlineData("wamwoowam@snug.moe", "wamwoowam", "snug.moe")]
    [InlineData("  @Wam@Snug.Moe  ", "Wam", "snug.moe")]
    [InlineData("snug.moe", null, "snug.moe")]
    [InlineData("https://snug.moe/", null, "snug.moe")]
    [InlineData("https://snug.moe", null, "snug.moe")]
    public void Handles_people_actually_type_parse(string input, string? username, string domain)
    {
        Assert.True(FediverseHandle.TryParse(input, out var handle));
        Assert.Equal(username, handle.Username);
        Assert.Equal(domain, handle.Domain);
        Assert.Equal(new Uri($"https://{domain}/"), handle.DomainRoot);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("@")]
    [InlineData("wam@")]
    [InlineData("wam @snug.moe")]
    [InlineData("localhost")]
    [InlineData("wam@localhost")]
    [InlineData("wam@internalhost")]
    [InlineData("wam@metadata.internal")]
    [InlineData("wam@printer.local")]
    [InlineData("wam@foo.localhost")]
    [InlineData("wam@router.home.arpa")]
    [InlineData("wam@nas.lan")]
    [InlineData("wam@127.0.0.1")]
    [InlineData("wam@10.0.0.5")]
    [InlineData("wam@[::1]")]
    [InlineData("wam@169.254.169.254")]
    [InlineData("wam@snug.moe.")]
    [InlineData("wam@localhost.")]
    [InlineData("wam@snug.moe:8080")]
    [InlineData("wam@snug.moe:443")]
    [InlineData("wam@snug.moe/api")]
    [InlineData("wam@snug.moe?x=1")]
    [InlineData("wam@snug.moe#frag")]
    [InlineData("evil.com\\@snug.moe")]
    [InlineData("wam/..@snug.moe")]
    [InlineData("wam%40evil.com@snug.moe")]
    [InlineData("http://snug.moe/")]
    [InlineData("https://snug.moe:8443/")]
    [InlineData("https://user:pass@snug.moe/")]
    [InlineData("https://snug.moe/@wamwoowam")]
    [InlineData("https://127.0.0.1/")]
    [InlineData("file:///etc/passwd")]
    [InlineData("gopher://snug.moe/")]
    public void Anything_that_isnt_a_public_https_host_is_refused(string input)
    {
        Assert.False(FediverseHandle.TryParse(input, out _));
    }

    [Fact]
    public void An_idn_domain_comes_back_as_punycode()
    {
        var unicodeDomain = "b" + (char)0xFC + "cher.example";

        Assert.True(FediverseHandle.TryParse($"wam@{unicodeDomain}", out var handle));
        Assert.Equal("xn--bcher-kva.example", handle.Domain);
    }

    [Fact]
    public void A_control_character_is_refused()
    {
        Assert.False(FediverseHandle.TryParse("wam@snug.moe\u0000", out _));
    }

    [Fact]
    public void A_bidi_override_is_refused()
    {
        var rightToLeftOverride = (char)0x202E;

        Assert.False(FediverseHandle.TryParse($"wam{rightToLeftOverride}@snug.moe", out _));
    }
}
