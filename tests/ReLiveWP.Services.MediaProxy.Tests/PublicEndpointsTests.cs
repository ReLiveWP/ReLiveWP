using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.MediaProxy.Endpoints;
using ReLiveWP.Services.MediaProxy.Services;
using SixLabors.ImageSharp;

namespace ReLiveWP.Services.MediaProxy.Tests;

public class PublicEndpointsTests : IDisposable
{
    private const string PhotoUrl = "https://cdn.example/photo.png";

    private readonly FakeUpstream upstream = new();
    private readonly ImagePipelineService pipeline = new(NullLogger<ImagePipelineService>.Instance);
    private readonly RemoteMediaService media;
    private readonly MediaProxyUrlSigner signer = SignedUrls.CreateSigner();

    public PublicEndpointsTests()
    {
        media = new RemoteMediaService(new HttpClient(upstream), pipeline, NullLogger<RemoteMediaService>.Instance);
    }

    public void Dispose() => pipeline.Dispose();

    private async Task<(IResult Result, HttpContext Context)> RequestAsync(SignedPath path)
    {
        var context = new DefaultHttpContext();
        var result = await PublicEndpoints.GetMediaAsync(path.Version, path.Size, path.Signature, path.Source,
                                                         context, signer, media, CancellationToken.None);
        return (result, context);
    }

    private SignedPath SignPhoto(MediaSize size)
    {
        var url = signer.SignUrl(new Uri(PhotoUrl), size);
        Assert.NotNull(url);
        return SignedUrls.SplitSignedUrl(url);
    }

    [Fact]
    public async Task ServesASignedImageWithLongCacheHeaders()
    {
        using var png = TestImages.CreatePng(400, 300, Color.CornflowerBlue);
        upstream.RespondWithBytes(PhotoUrl, png.ToArray(), "image/png");

        var (result, context) = await RequestAsync(SignPhoto(MediaSize.Thumb));

        var file = Assert.IsType<FileContentHttpResult>(result);
        Assert.Equal("image/jpeg", file.ContentType);
        Assert.NotNull(file.EntityTag);
        Assert.Equal(PublicEndpoints.ServedCacheControl, context.Response.Headers.CacheControl.ToString());
        Assert.Equal("nosniff", context.Response.Headers.XContentTypeOptions.ToString());
    }

    [Fact]
    public async Task ABadSignatureIsANotFoundWithAShortCache()
    {
        var path = SignPhoto(MediaSize.Thumb) with { Size = "full" };

        var (result, context) = await RequestAsync(path);

        Assert.IsType<NotFound>(result);
        Assert.Equal(PublicEndpoints.NotFoundCacheControl, context.Response.Headers.CacheControl.ToString());
        Assert.Empty(upstream.Requests);
    }

    [Fact]
    public async Task AnUpstreamFailureIsABadGatewayWithAShortCache()
    {
        var (result, context) = await RequestAsync(SignPhoto(MediaSize.Thumb));

        var status = Assert.IsType<StatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, status.StatusCode);
        Assert.Equal(PublicEndpoints.UpstreamFailureCacheControl, context.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task SomethingThatIsNotAnImageIsCachedLongerThanAFailure()
    {
        upstream.RespondWithBytes(PhotoUrl, "<html></html>"u8.ToArray(), "text/html");

        var (result, context) = await RequestAsync(SignPhoto(MediaSize.Thumb));

        var status = Assert.IsType<StatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, status.StatusCode);
        Assert.Equal(PublicEndpoints.RejectedCacheControl, context.Response.Headers.CacheControl.ToString());
    }
}
