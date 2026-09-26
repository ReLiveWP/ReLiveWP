using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ReLiveWP.ServiceDefaults.Media;
using SixLabors.ImageSharp;

namespace ReLiveWP.Services.MediaProxy.Tests;

public class MediaProxyHostTests(MediaProxyHost host) : IClassFixture<MediaProxyHost>
{
    private static StreamContent CreateBody(Stream source)
    {
        var content = new StreamContent(source);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return content;
    }

    [Fact]
    public async Task TheInternalListenerProcessesAnImage()
    {
        using var client = host.CreateInternalClient();
        using var source = TestImages.CreateJpeg(400, 300);

        using var response = await client.PostAsync("/internal/v1/process?profile=thumb:96", CreateBody(source));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);

        using var produced = Image.Load(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(96, produced.Width);
    }

    [Fact]
    public async Task TheInternalRouteIsNotFoundOnThePublicListener()
    {
        using var client = host.CreatePublicClient();
        using var source = TestImages.CreateJpeg(400, 300);

        using var response = await client.PostAsync("/internal/v1/process?profile=thumb:96", CreateBody(source));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AForgedHostDoesNotGetThePublicListenerIntoTheInternalRoute()
    {
        using var client = host.CreatePublicClient();
        using var source = TestImages.CreateJpeg(400, 300);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/internal/v1/process?profile=thumb:96")
        {
            Content = CreateBody(source),
        };
        request.Headers.Host = $"127.0.0.1:{host.InternalPort}";

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AnUnknownProfileIsABadRequestWithACode()
    {
        using var client = host.CreateInternalClient();
        using var source = TestImages.CreateJpeg(10, 10);

        using var response = await client.PostAsync("/internal/v1/process?profile=huge", CreateBody(source));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var rejection = await response.Content.ReadFromJsonAsync<MediaPipelineRejection>();
        Assert.Equal(MediaPipelineRejection.UnknownProfile, rejection?.Code);
    }

    // kestrel's own limit is 30 MB, so without lifting it this would be a 413 from the server
    // rather than the pipeline's answer
    [Fact]
    public async Task ABodyOverKestrelsDefaultLimitReachesThePipeline()
    {
        using var client = host.CreateInternalClient();
        using var source = new ZeroFilledStream(31L * 1024 * 1024);

        using var response = await client.PostAsync("/internal/v1/process?profile=thumb:96", CreateBody(source));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var rejection = await response.Content.ReadFromJsonAsync<MediaPipelineRejection>();
        Assert.Equal(MediaPipelineRejection.Unreadable, rejection?.Code);
    }

    [Fact]
    public async Task ABadSignatureOnThePublicListenerIsANotFoundWithAShortCache()
    {
        using var client = host.CreatePublicClient();

        using var response = await client.GetAsync("/v2/thumb/AAAAAAAAAAAAAAAAAAAAAA/aHR0cHM6Ly9jZG4uZXhhbXBsZS9hLnBuZw.jpg");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("public, max-age=60, s-maxage=60", response.Headers.CacheControl?.ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HealthAnswersOnBothListeners(bool onPublic)
    {
        using var client = onPublic ? host.CreatePublicClient() : host.CreateInternalClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public class MediaProxyStartupTests
{
    [Fact]
    public void RefusesToStartWithBothListenersOnOnePort()
    {
        var error = Assert.ThrowsAny<Exception>(() =>
        {
            using var host = MediaProxyHost.StartOnPorts(publicPort: 5999, internalPort: 5999);
        });

        Assert.Contains("must listen on different ports", error.ToString());
    }
}
