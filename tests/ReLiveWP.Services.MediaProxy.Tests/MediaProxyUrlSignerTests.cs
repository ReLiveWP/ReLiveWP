using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using ReLiveWP.ServiceDefaults.Media;

namespace ReLiveWP.Services.MediaProxy.Tests;

public class MediaProxyUrlSignerTests
{
    private static readonly Uri Avatar = new("https://cdn.bsky.app/img/avatar/plain/did:plc:abc/bafkrei@jpeg");

    private static bool TryVerify(MediaProxyUrlSigner signer, SignedPath path, out Uri source, out MediaSize size)
        => signer.TryVerifyPath(path.Version, path.Size, path.Signature, path.Source, out source, out size);

    private static string ForgeSignature(string key, string version, string size, string source)
    {
        var mac = HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes($"{version}\n{size}\n{source}"));
        return WebEncoders.Base64UrlEncode(mac[..MediaProxyUrlSigner.SignatureBytes]);
    }

    [Theory]
    [InlineData(MediaSize.Avatar)]
    [InlineData(MediaSize.Thumb)]
    [InlineData(MediaSize.Full)]
    public void RoundTripsEverySize(MediaSize size)
    {
        var signer = SignedUrls.CreateSigner();

        var url = signer.SignUrl(Avatar, size);

        Assert.NotNull(url);
        Assert.StartsWith("https://media.example/v2/", url);
        Assert.True(TryVerify(signer, SignedUrls.SplitSignedUrl(url), out var source, out var verifiedSize));
        Assert.Equal(Avatar, source);
        Assert.Equal(size, verifiedSize);
    }

    [Fact]
    public void KeepsTheWholeUrlInThePathWithNoQuery()
    {
        var signer = SignedUrls.CreateSigner();
        var withQuery = new Uri("https://files.mastodon.social/media_attachments/1/original/a.png?1700000000");

        var url = signer.SignUrl(withQuery, MediaSize.Thumb)!;

        Assert.Empty(new Uri(url).Query);
        Assert.True(TryVerify(signer, SignedUrls.SplitSignedUrl(url), out var source, out _));
        Assert.Equal(withQuery, source);
    }

    [Fact]
    public void RefusesATamperedSize()
    {
        var signer = SignedUrls.CreateSigner();
        var path = SignedUrls.SplitSignedUrl(signer.SignUrl(Avatar, MediaSize.Avatar)!);

        Assert.False(TryVerify(signer, path with { Size = "full" }, out _, out _));
    }

    [Fact]
    public void RefusesATamperedSignature()
    {
        var signer = SignedUrls.CreateSigner();
        var path = SignedUrls.SplitSignedUrl(signer.SignUrl(Avatar, MediaSize.Avatar)!);
        var flipped = (path.Signature[0] == 'A' ? 'B' : 'A') + path.Signature[1..];

        Assert.False(TryVerify(signer, path with { Signature = flipped }, out _, out _));
    }

    [Fact]
    public void RefusesATamperedSource()
    {
        var signer = SignedUrls.CreateSigner();
        var path = SignedUrls.SplitSignedUrl(signer.SignUrl(Avatar, MediaSize.Avatar)!);
        var other = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("https://cdn.bsky.app/img/avatar/plain/did:plc:xyz/other@jpeg"));

        Assert.False(TryVerify(signer, path with { Source = other }, out _, out _));
    }

    [Fact]
    public void RefusesASignatureMovedToAnotherVersion()
    {
        var signer = SignedUrls.CreateSigner();
        var path = SignedUrls.SplitSignedUrl(signer.SignUrl(Avatar, MediaSize.Avatar)!);

        Assert.False(TryVerify(signer, path with { Version = "v1" }, out _, out _));
    }

    [Fact]
    public void AcceptsAPreviousKeyThatIsStillConfigured()
    {
        var signer = SignedUrls.CreateSigner();
        var source = Avatar.AbsoluteUri;
        var path = new SignedPath("v1", "thumb", ForgeSignature(SignedUrls.PreviousKey, "v1", "thumb", source),
                                  WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(source)));

        Assert.True(TryVerify(signer, path, out _, out _));
    }

    [Fact]
    public void RefusesARetiredKey()
    {
        var current = new MediaProxyUrlSigner(new Dictionary<string, string> { ["v2"] = SignedUrls.CurrentKey }, SignedUrls.PublicRoot);
        var source = Avatar.AbsoluteUri;
        var path = new SignedPath("v1", "thumb", ForgeSignature(SignedUrls.PreviousKey, "v1", "thumb", source),
                                  WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(source)));

        Assert.False(TryVerify(current, path, out _, out _));
    }

    [Theory]
    [InlineData("http://cdn.bsky.app/a.jpg")]
    [InlineData("https://cdn.bsky.app:8443/a.jpg")]
    [InlineData("https://10.0.0.1/a.jpg")]
    [InlineData("https://nas.local/a.jpg")]
    [InlineData("https://user:pass@cdn.bsky.app/a.jpg")]
    [InlineData("https://localhost/a.jpg")]
    public void RefusesToSignAnUnacceptableSource(string source)
    {
        Assert.Null(SignedUrls.CreateSigner().SignUrl(new Uri(source), MediaSize.Thumb));
    }

    [Theory]
    [InlineData("http://cdn.bsky.app/a.jpg")]
    [InlineData("https://10.0.0.1/a.jpg")]
    [InlineData("https://metadata.internal/latest")]
    public void RefusesAnUnacceptableSourceEvenWithAValidSignature(string source)
    {
        var path = new SignedPath("v2", "thumb", ForgeSignature(SignedUrls.CurrentKey, "v2", "thumb", source),
                                  WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(source)));

        Assert.False(TryVerify(SignedUrls.CreateSigner(), path, out _, out _));
    }

    [Theory]
    [InlineData("v3", "thumb", "AAAAAAAAAAAAAAAAAAAAAA", "aHR0cHM6Ly9hLmIvYw")]
    [InlineData("v2", "huge", "AAAAAAAAAAAAAAAAAAAAAA", "aHR0cHM6Ly9hLmIvYw")]
    [InlineData("v2", "thumb", "not base64!", "aHR0cHM6Ly9hLmIvYw")]
    [InlineData("v2", "thumb", "AAAA", "aHR0cHM6Ly9hLmIvYw")]
    [InlineData("v2", "thumb", "AAAAAAAAAAAAAAAAAAAAAA", "%%%")]
    public void RefusesJunkPaths(string version, string size, string signature, string source)
    {
        Assert.False(SignedUrls.CreateSigner().TryVerifyPath(version, size, signature, source, out _, out _));
    }

    [Fact]
    public void RefusesAnOversizedSourceBeforeHashingIt()
    {
        var huge = new string('A', MediaProxyUrlSigner.MaxEncodedSourceLength + 1);

        Assert.False(SignedUrls.CreateSigner().TryVerifyPath("v2", "thumb", "AAAAAAAAAAAAAAAAAAAAAA", huge, out _, out _));
    }

    [Fact]
    public void RefusesToSignASourceItWouldRefuseToVerify()
    {
        var signer = SignedUrls.CreateSigner();
        var longest = new Uri($"https://cdn.example/{new string('a', 3000)}.png");
        var tooLong = new Uri($"https://cdn.example/{new string('a', 3100)}.png");

        var signed = signer.SignUrl(longest, MediaSize.Thumb);

        Assert.NotNull(signed);
        Assert.True(TryVerify(signer, SignedUrls.SplitSignedUrl(signed), out _, out _));
        Assert.Null(signer.SignUrl(tooLong, MediaSize.Thumb));
        Assert.Equal(tooLong.AbsoluteUri, signer.SignUrlOrOriginal(tooLong.AbsoluteUri, MediaSize.Thumb));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("/relative/avatar.png")]
    [InlineData("http://cdn.bsky.app/a.jpg")]
    [InlineData("https://nas.local/a.jpg")]
    public void HandsBackWhatItCannotSign(string url)
    {
        Assert.Equal(url, SignedUrls.CreateSigner().SignUrlOrOriginal(url, MediaSize.Avatar));
    }

    [Fact]
    public void SignsWhatItCan()
    {
        var signed = SignedUrls.CreateSigner().SignUrlOrOriginal(Avatar.AbsoluteUri, MediaSize.Avatar);

        Assert.StartsWith("https://media.example/v2/avatar/", signed);
    }

    [Fact]
    public void SignsAPathThatVerifiesWithoutAPublicRoot()
    {
        var signer = new MediaProxyUrlSigner(new Dictionary<string, string> { ["v1"] = SignedUrls.CurrentKey }, null);

        var path = signer.SignPath(Avatar, MediaSize.Thumb);

        Assert.NotNull(path);
        Assert.StartsWith("/v1/thumb/", path);
        Assert.True(TryVerify(signer, SignedUrls.SplitSignedUrl($"https://anywhere.example{path}"), out var source, out _));
        Assert.Equal(Avatar, source);
    }

    [Theory]
    [InlineData(0, MediaSize.Full)]
    [InlineData(-1, MediaSize.Full)]
    [InlineData(48, MediaSize.Avatar)]
    [InlineData(96, MediaSize.Avatar)]
    [InlineData(97, MediaSize.Thumb)]
    [InlineData(176, MediaSize.Thumb)]
    [InlineData(320, MediaSize.Thumb)]
    [InlineData(321, MediaSize.Full)]
    [InlineData(800, MediaSize.Full)]
    public void ChoosesTheSmallestProxySizeThatCoversTheRequest(int requested, MediaSize expected)
    {
        Assert.Equal(expected, MediaSizes.ChooseSizeFor(requested));
    }

    [Fact]
    public void SignsNothingWithoutAKey()
    {
        var signer = new MediaProxyUrlSigner(new Dictionary<string, string>(), SignedUrls.PublicRoot);

        Assert.False(signer.CanVerify);
        Assert.Null(signer.SignUrl(Avatar, MediaSize.Thumb));
    }

    [Fact]
    public void SignsNothingWithoutAPublicRoot()
    {
        var signer = new MediaProxyUrlSigner(new Dictionary<string, string> { ["v1"] = SignedUrls.CurrentKey }, null);

        Assert.True(signer.CanVerify);
        Assert.Null(signer.SignUrl(Avatar, MediaSize.Thumb));
    }

    [Fact]
    public void RefusesAShortKey()
    {
        Assert.Throws<InvalidOperationException>(
            () => new MediaProxyUrlSigner(new Dictionary<string, string> { ["v1"] = "short" }, SignedUrls.PublicRoot));
    }

    [Fact]
    public void RefusesAKeyThatIsNotVersioned()
    {
        Assert.Throws<InvalidOperationException>(
            () => new MediaProxyUrlSigner(new Dictionary<string, string> { ["latest"] = SignedUrls.CurrentKey }, SignedUrls.PublicRoot));
    }

    [Fact]
    public void ReadsKeysAndRootFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Media:ProxyKeys:v1"] = SignedUrls.PreviousKey,
                ["Media:ProxyKeys:v10"] = SignedUrls.CurrentKey,
                ["PublicUrls:Media"] = "https://media.example/",
            })
            .Build();

        var url = MediaProxyUrlSigner.FromConfiguration(configuration).SignUrl(Avatar, MediaSize.Thumb);

        Assert.NotNull(url);
        Assert.StartsWith("https://media.example/v10/thumb/", url);
    }
}
