using ReLiveWP.ServiceDefaults.Media;

namespace ReLiveWP.Services.MediaProxy.Tests;

public record SignedPath(string Version, string Size, string Signature, string Source);

internal static class SignedUrls
{
    public const string CurrentKey = "current-key-current-key-current-key-0123";
    public const string PreviousKey = "previous-key-previous-key-previous-key-01";

    public static readonly Uri PublicRoot = new("https://media.example");

    public static MediaProxyUrlSigner CreateSigner()
    {
        var secrets = new Dictionary<string, string> { ["v1"] = PreviousKey, ["v2"] = CurrentKey };
        return new MediaProxyUrlSigner(secrets, PublicRoot);
    }

    public static SignedPath SplitSignedUrl(string url)
    {
        var path = new Uri(url).AbsolutePath;
        Assert.EndsWith(MediaProxyUrlSigner.FileExtension, path);

        var trimmed = path[1..^MediaProxyUrlSigner.FileExtension.Length];
        var segments = trimmed.Split('/');
        Assert.Equal(4, segments.Length);

        return new SignedPath(segments[0], segments[1], segments[2], segments[3]);
    }
}
