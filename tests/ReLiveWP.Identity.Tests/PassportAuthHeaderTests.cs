using System.Net.Http.Headers;
using System.Web;
using ReLiveWP.Identity.LiveID;

namespace ReLiveWP.Identity.Tests;

public class PassportAuthHeaderTests
{
    private const string Jwt = "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJkb2NzIn0.sig";

    [Fact]
    public void OfficeMobileHeaderYieldsTheJwt()
    {
        var header = $"Passport1.4 from-PP='t={Jwt}&p='";

        Assert.True(AuthenticationHeaderValue.TryParse(header, out var parsed));
        Assert.Equal("Passport1.4", parsed!.Scheme);
        Assert.True(PassportToken.TryUnwrap(parsed.Parameter!, out var inner));
        Assert.Equal(Jwt, HttpUtility.ParseQueryString(inner)["t"]);
    }

    [Fact]
    public void EmptyTrailingParameterIsTolerated()
    {
        Assert.True(PassportToken.TryUnwrap($"from-PP='t={Jwt}&p='", out var inner));
        Assert.Equal($"t={Jwt}&p=", inner);
    }

    [Fact]
    public void TrailingCredentialsAfterTheQuotedBlockAreIgnored()
    {
        Assert.True(PassportToken.TryUnwrap($"from-PP='t={Jwt}&p=',ru=http://example.com", out var inner));
        Assert.Equal($"t={Jwt}&p=", inner);
    }

    [Fact]
    public void DocumentOpenPathOmitsTheEqualsAndOpeningQuote()
    {
        Assert.True(PassportToken.TryUnwrap($"from-PPt={Jwt}&p='", out var inner));
        Assert.Equal($"t={Jwt}&p=", inner);
        Assert.Equal(Jwt, HttpUtility.ParseQueryString(inner)["t"]);
    }

    [Fact]
    public void UnterminatedValueIsStillUsable()
    {
        Assert.True(PassportToken.TryUnwrap($"from-PP='t={Jwt}&p=", out var inner));
        Assert.Equal($"t={Jwt}&p=", inner);
    }

    [Theory]
    [InlineData("from-pp='t=abc'", "t=abc")]
    [InlineData("FROM-PP='t=abc'", "t=abc")]
    [InlineData("from-PP=t=abc", "t=abc")]
    public void MarkerMatchIsCaseInsensitive(string header, string expected)
    {
        Assert.True(PassportToken.TryUnwrap(header, out var inner));
        Assert.Equal(expected, inner);
    }

    [Theory]
    [InlineData("")]
    [InlineData("t=abc&p=")]
    [InlineData("from-PP=")]
    [InlineData("from-PP=''")]
    public void NonWrappedValuesArePassedThrough(string header)
        => Assert.False(PassportToken.TryUnwrap(header, out _));
}
