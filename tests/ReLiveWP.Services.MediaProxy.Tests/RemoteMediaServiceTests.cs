using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.ServiceDefaults.Outbound;
using ReLiveWP.Services.MediaProxy.Services;
using SixLabors.ImageSharp;

namespace ReLiveWP.Services.MediaProxy.Tests;

public class RemoteMediaServiceTests : IDisposable
{
    private const string PhotoUrl = "https://cdn.example/photo.png";

    private readonly FakeUpstream upstream = new();
    private readonly HttpClient http;
    private readonly ImagePipelineService pipeline = new(NullLogger<ImagePipelineService>.Instance);

    public RemoteMediaServiceTests()
    {
        http = new HttpClient(upstream);
    }

    public void Dispose() => pipeline.Dispose();

    private RemoteMediaService CreateService()
    {
        return new RemoteMediaService(http, pipeline, NullLogger<RemoteMediaService>.Instance);
    }

    private RemoteMediaService CreateService(TimeSpan deadline)
    {
        return new RemoteMediaService(http, pipeline, NullLogger<RemoteMediaService>.Instance)
        {
            FetchDeadline = deadline,
        };
    }

    private static byte[] CreatePngBytes(int width, int height)
    {
        using var png = TestImages.CreatePng(width, height, Color.CornflowerBlue);
        return png.ToArray();
    }

    [Theory]
    [InlineData(MediaSize.Avatar, 96, 72)]
    [InlineData(MediaSize.Thumb, 320, 240)]
    [InlineData(MediaSize.Full, 1024, 768)]
    public async Task FetchesAndRendersEachSize(MediaSize size, int width, int height)
    {
        upstream.RespondWithBytes(PhotoUrl, CreatePngBytes(2000, 1500), "image/png");

        var result = await CreateService().RenderMediaAsync(new Uri(PhotoUrl), size);

        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.NotNull(result.Jpeg);
        Assert.NotNull(result.ETag);

        using var produced = Image.Load(result.Jpeg);
        Assert.Equal(width, produced.Width);
        Assert.Equal(height, produced.Height);
    }

    [Fact]
    public async Task AsksForTheImageTypesThePipelineReads()
    {
        upstream.RespondWithBytes(PhotoUrl, CreatePngBytes(50, 50), "image/png");

        await CreateService().RenderMediaAsync(new Uri(PhotoUrl), MediaSize.Thumb);

        var request = Assert.Single(upstream.Requests);
        Assert.Contains(request.Headers.Accept, accept => accept.MediaType == "image/webp");
    }

    [Fact]
    public void TheRemoteClientIsGuardedAndNamesItself()
    {
        using var handler = RemoteMediaService.CreateRemoteHandler(new PublicOnlyAddressPolicy());
        using var client = RemoteMediaService.CreateRemoteClient(new PublicOnlyAddressPolicy());

        Assert.False(handler.AllowAutoRedirect);
        Assert.NotNull(handler.ConnectCallback);
        Assert.NotEqual(Timeout.InfiniteTimeSpan, handler.PooledConnectionLifetime);
        Assert.Equal(RemoteMediaService.UserAgent, client.DefaultRequestHeaders.UserAgent.ToString());
    }

    // localhost resolves without the network, so this exercises the real connect-time refusal
    [Fact]
    public async Task TheRemoteClientRefusesAPrivateAddressAtConnectTime()
    {
        using var client = RemoteMediaService.CreateRemoteClient(new PublicOnlyAddressPolicy());

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("http://localhost:1/"));

        Assert.Contains("does not resolve to a public address", error.Message);
    }

    [Fact]
    public async Task SniffsTheContentRatherThanTrustingTheContentType()
    {
        upstream.RespondWithBytes(PhotoUrl, CreatePngBytes(50, 50), "text/html");

        var result = await CreateService().RenderMediaAsync(new Uri(PhotoUrl), MediaSize.Thumb);

        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
    }

    [Fact]
    public async Task RefusesSomethingThatIsNotAnImageAsUnsupported()
    {
        upstream.RespondWithBytes(PhotoUrl, "<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8.ToArray(), "image/svg+xml");

        var result = await CreateService().RenderMediaAsync(new Uri(PhotoUrl), MediaSize.Thumb);

        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, result.StatusCode);
        Assert.Null(result.Jpeg);
    }

    [Fact]
    public async Task FollowsARedirectToAnotherPublicHost()
    {
        upstream.RespondWithRedirect(PhotoUrl, "https://files.example/moved.png");
        upstream.RespondWithBytes("https://files.example/moved.png", CreatePngBytes(50, 50), "image/png");

        var result = await CreateService().RenderMediaAsync(new Uri(PhotoUrl), MediaSize.Thumb);

        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(2, upstream.Requests.Count);
    }

    [Theory]
    [InlineData("https://nas.local/photo.png")]
    [InlineData("https://metadata.internal/latest")]
    [InlineData("http://files.example/photo.png")]
    [InlineData("https://10.0.0.5/photo.png")]
    [InlineData("https://files.example:8443/photo.png")]
    public async Task RefusesARedirectSomewhereItWouldNeverSign(string location)
    {
        upstream.RespondWithRedirect(PhotoUrl, location);
        upstream.Respond(location, (_, _) => throw new InvalidOperationException("must never be requested"));

        var result = await CreateService().RenderMediaAsync(new Uri(PhotoUrl), MediaSize.Thumb);

        Assert.Equal(StatusCodes.Status502BadGateway, result.StatusCode);
        Assert.Single(upstream.Requests);
    }

    [Fact]
    public async Task GivesUpAfterTooManyRedirects()
    {
        for (var hop = 0; hop < 10; hop++)
            upstream.RespondWithRedirect($"https://cdn.example/hop{hop}.png", $"https://cdn.example/hop{hop + 1}.png");

        var result = await CreateService().RenderMediaAsync(new Uri("https://cdn.example/hop0.png"), MediaSize.Thumb);

        Assert.Equal(StatusCodes.Status502BadGateway, result.StatusCode);
        Assert.Equal(ExternalRequestGuard.MaxRedirects + 1, upstream.Requests.Count);
    }

    [Fact]
    public async Task TreatsAnUpstreamErrorAsABadGateway()
    {
        upstream.Respond(PhotoUrl, (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Gone)));

        var result = await CreateService().RenderMediaAsync(new Uri(PhotoUrl), MediaSize.Thumb);

        Assert.Equal(StatusCodes.Status502BadGateway, result.StatusCode);
    }

    [Fact]
    public async Task TreatsAConnectionFailureAsABadGateway()
    {
        upstream.Respond(PhotoUrl, (_, _) => throw new HttpRequestException("does not resolve to a public address"));

        var result = await CreateService().RenderMediaAsync(new Uri(PhotoUrl), MediaSize.Thumb);

        Assert.Equal(StatusCodes.Status502BadGateway, result.StatusCode);
    }

    [Fact]
    public async Task RefusesADeclaredLengthOverTheCapWithoutReadingIt()
    {
        var body = new ZeroFilledStream(RemoteMediaService.MaxRemoteBytes + 1L);
        upstream.Respond(PhotoUrl, (_, _) =>
            Task.FromResult(FakeUpstream.CreateStreamResponse(body, RemoteMediaService.MaxRemoteBytes + 1L)));

        var result = await CreateService().RenderMediaAsync(new Uri(PhotoUrl), MediaSize.Thumb);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
        Assert.Equal(0, body.Position);
    }

    [Fact]
    public async Task TripsTheCapMidStreamWhenNoLengthIsDeclared()
    {
        var body = new ZeroFilledStream(RemoteMediaService.MaxRemoteBytes * 3L);
        upstream.Respond(PhotoUrl, (_, _) => Task.FromResult(FakeUpstream.CreateStreamResponse(body)));

        var result = await CreateService().RenderMediaAsync(new Uri(PhotoUrl), MediaSize.Thumb);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
        Assert.True(body.Position < RemoteMediaService.MaxRemoteBytes + 1024 * 1024);
    }

    [Fact]
    public async Task AnUpstreamThatStallsAfterItsHeadersHitsTheDeadline()
    {
        upstream.Respond(PhotoUrl, (_, _) => Task.FromResult(FakeUpstream.CreateStreamResponse(new StallingStream())));

        var result = await CreateService(TimeSpan.FromMilliseconds(200)).RenderMediaAsync(new Uri(PhotoUrl), MediaSize.Thumb);

        Assert.Equal(StatusCodes.Status502BadGateway, result.StatusCode);
    }

    [Fact]
    public async Task ConcurrentMissesForOneImageShareOneFetch()
    {
        var release = new TaskCompletionSource();
        var png = CreatePngBytes(50, 50);
        upstream.Respond(PhotoUrl, async (_, _) =>
        {
            await release.Task;
            return FakeUpstream.CreateBytesResponse(png, "image/png");
        });

        var service = CreateService();
        var first = service.RenderMediaAsync(new Uri(PhotoUrl), MediaSize.Thumb);
        var second = service.RenderMediaAsync(new Uri(PhotoUrl), MediaSize.Thumb);
        var otherSize = service.RenderMediaAsync(new Uri(PhotoUrl), MediaSize.Avatar);

        release.SetResult();
        var results = await Task.WhenAll(first, second, otherSize);

        Assert.All(results, result => Assert.Equal(StatusCodes.Status200OK, result.StatusCode));
        Assert.Same(results[0], results[1]);
        Assert.Equal(2, upstream.Requests.Count);
    }

    [Fact]
    public async Task OneCallerHangingUpDoesNotCancelTheSharedFetch()
    {
        var release = new TaskCompletionSource();
        var png = CreatePngBytes(50, 50);
        upstream.Respond(PhotoUrl, async (_, _) =>
        {
            await release.Task;
            return FakeUpstream.CreateBytesResponse(png, "image/png");
        });

        var service = CreateService();
        using var impatient = new CancellationTokenSource();
        var abandoned = service.RenderMediaAsync(new Uri(PhotoUrl), MediaSize.Thumb, impatient.Token);
        var patient = service.RenderMediaAsync(new Uri(PhotoUrl), MediaSize.Thumb);

        await impatient.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => abandoned);

        release.SetResult();
        var result = await patient;

        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
    }
}
