using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.Activity.Providers;
using ReLiveWP.Services.Activity.Providers.Bluesky;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Activity.Utilities;

namespace ReLiveWP.Services.Activity.Tests;

public class AlbumRenderServiceTests
{
    private const string Did = "did:plc:amyamyamyamyamyamyamy";
    private const string BlobCid = "bafkreiacbosk5hldxy";

    private static readonly string PhotoRef = SocialAlbumRef.ForPhoto("atproto", Did, BlobCid);

    private static AlbumRenderService CreateRenderer(MediaProxyUrlSigner mediaProxy)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Media:TicketSecret"] = "ticket-secret" })
            .Build();

        var tickets = new MediaTicketService(configuration, TimeProvider.System);
        var albums = new SocialAlbumsService([new BlueskyAlbumProvider(null!, TestCache.New(), NullLoggerFactory.Instance)]);
        return new AlbumRenderService(tickets, albums, mediaProxy);
    }

    private static string DecodeProxiedSource(string url)
    {
        var encoded = new Uri(url).AbsolutePath.Split('/')[4].Replace(MediaProxyUrlSigner.FileExtension, "");
        return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded));
    }

    private static SocialAlbumFolder CreateFolder()
    {
        var photo = new SocialPhoto(PhotoRef, $"{BlobCid}.jpg", "a cat", new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc), 1000, 750);
        return new SocialAlbumFolder("@amy.example's photos", [photo]);
    }

    [Fact]
    public async Task SocialAlbumPhotosPointAtTheMediaProxy()
    {
        var response = await CreateRenderer(TestMediaProxy.CreateSigner()).RenderSocialLibraryAsync($"atproto+{Did}", CreateFolder());

        var photo = Assert.Single(response.Photos);
        Assert.StartsWith($"{TestMediaProxy.Root}/v1/thumb/", photo.ThumbnailUrl);
        Assert.StartsWith($"{TestMediaProxy.Root}/v1/full/", photo.FullSizeUrl);
        Assert.Equal($"https://cdn.bsky.app/img/feed_thumbnail/plain/{Did}/{BlobCid}@jpeg", DecodeProxiedSource(photo.ThumbnailUrl));
        Assert.Equal($"https://cdn.bsky.app/img/feed_fullsize/plain/{Did}/{BlobCid}@jpeg", DecodeProxiedSource(photo.FullSizeUrl));
    }

    [Fact]
    public async Task ASocialCoverPointsAtTheMediaProxy()
    {
        var summary = await CreateRenderer(TestMediaProxy.CreateSigner()).RenderSocialSummaryAsync(new SocialAlbum($"atproto+{Did}", "photos"), PhotoRef);

        Assert.NotNull(summary.CoverUrl);
        Assert.StartsWith($"{TestMediaProxy.Root}/v1/thumb/", summary.CoverUrl);
    }

    [Fact]
    public async Task WithoutAProxyKeyTheWebGetsTheCdnDirectly()
    {
        var response = await CreateRenderer(TestMediaProxy.Unconfigured).RenderSocialLibraryAsync($"atproto+{Did}", CreateFolder());

        var photo = Assert.Single(response.Photos);
        Assert.Equal($"https://cdn.bsky.app/img/feed_thumbnail/plain/{Did}/{BlobCid}@jpeg", photo.ThumbnailUrl);
        Assert.Equal($"https://cdn.bsky.app/img/feed_fullsize/plain/{Did}/{BlobCid}@jpeg", photo.FullSizeUrl);
    }

    [Fact]
    public async Task AnUnresolvableCoverIsLeftOut()
    {
        var summary = await CreateRenderer(TestMediaProxy.CreateSigner()).RenderSocialSummaryAsync(new SocialAlbum($"atproto+{Did}", "photos"), "nostr+x+y");

        Assert.Null(summary.CoverUrl);
    }
}
