using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.ServiceDefaults.Media;

namespace ReLiveWP.Services.MediaProxy.Tests;

public class MediaPipelineClientTests
{
    private static readonly Uri InternalEndpoint = new("http://mediaproxy:5000");

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => respond(request, ct);
    }

    private static MediaPipelineClient CreateClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    {
        var http = new HttpClient(new StubHandler(respond)) { BaseAddress = InternalEndpoint };
        return new MediaPipelineClient(http, NullLogger<MediaPipelineClient>.Instance);
    }

    [Fact]
    public async Task PostsTheSourceToTheProcessRouteAndReturnsTheJpeg()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xD9];
        byte[] sent = [];
        HttpRequestMessage? captured = null;

        var client = CreateClient(async (request, ct) =>
        {
            captured = request;
            sent = await request.Content!.ReadAsByteArrayAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(jpeg) };
        });

        using var source = new MemoryStream([1, 2, 3, 4, 5]);
        var result = await client.ProcessImageAsync(source, MediaProfiles.ForThumbnail(176));

        Assert.Equal(jpeg, result.Jpeg);
        Assert.Null(result.Rejection);
        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured.Method);
        Assert.Equal("/internal/v1/process", captured.RequestUri!.AbsolutePath);
        Assert.Equal("?profile=thumb%3A176", captured.RequestUri.Query);
        Assert.Equal([1, 2, 3, 4, 5], sent);
    }

    [Fact]
    public async Task DeclaresTheLengthOfAStreamThatCannotSeek()
    {
        long? declared = null;

        var client = CreateClient((request, _) =>
        {
            declared = request.Content!.Headers.ContentLength;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([0xFF]) });
        });

        using var source = new ZeroFilledStream(10);
        await client.ProcessImageAsync(source, MediaProfiles.ForThumbnail(96), 10);

        Assert.Equal(10, declared);
    }

    [Fact]
    public async Task CarriesTheRefusalCodeAndDetail()
    {
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = JsonContent.Create(new MediaPipelineRejection(MediaPipelineRejection.TooManyPixels, "9000x9000")),
        }));

        using var source = new MemoryStream([1]);
        var result = await client.ProcessImageAsync(source, MediaProfiles.ForThumbnail(96));

        Assert.Null(result.Jpeg);
        Assert.False(result.IsUnavailable);
        Assert.Equal(MediaPipelineRejection.TooManyPixels, result.Rejection?.Code);
        Assert.Equal("9000x9000", result.Rejection?.Detail);
    }

    [Fact]
    public async Task AnErrorThatIsNotARefusalIsUnavailable()
    {
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("<html>bad gateway</html>"),
        }));

        using var source = new MemoryStream([1]);

        Assert.True((await client.ProcessImageAsync(source, MediaProfiles.ForThumbnail(96))).IsUnavailable);
    }

    [Fact]
    public async Task AFiveHundredWithJsonIsStillUnavailable()
    {
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = JsonContent.Create(new MediaPipelineRejection("whatever")),
        }));

        using var source = new MemoryStream([1]);

        Assert.True((await client.ProcessImageAsync(source, MediaProfiles.ForThumbnail(96))).IsUnavailable);
    }

    [Fact]
    public async Task IsUnavailableWhenThePipelineIsDown()
    {
        var client = CreateClient((_, _) => throw new HttpRequestException("connection refused"));

        using var source = new MemoryStream([1]);

        Assert.True((await client.ProcessImageAsync(source, MediaProfiles.ForThumbnail(96))).IsUnavailable);
    }

    [Fact]
    public async Task IsUnavailableWhenThePipelineTimesOut()
    {
        var client = CreateClient((_, _) => throw new TaskCanceledException("timed out"));

        using var source = new MemoryStream([1]);

        Assert.True((await client.ProcessImageAsync(source, MediaProfiles.ForThumbnail(96))).IsUnavailable);
    }

    [Fact]
    public async Task LetsTheCallersOwnCancellationThrough()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var client = CreateClient((_, ct) => Task.FromCanceled<HttpResponseMessage>(ct));

        using var source = new MemoryStream([1]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.ProcessImageAsync(source, MediaProfiles.ForThumbnail(96), cancelled.Token));
    }
}
