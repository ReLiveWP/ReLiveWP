using ReLiveWP.ServiceDefaults.Media;

namespace ReLiveWP.Services.Activity.Tests;

internal static class TestMediaProxy
{
    public const string Root = "https://media.example";

    public static MediaProxyUrlSigner Unconfigured { get; } = new(new Dictionary<string, string>(), null);

    public static MediaProxyUrlSigner CreateSigner()
    {
        var secrets = new Dictionary<string, string> { ["v1"] = "test-media-proxy-key-test-media-proxy-key" };
        return new MediaProxyUrlSigner(secrets, new Uri(Root));
    }
}
