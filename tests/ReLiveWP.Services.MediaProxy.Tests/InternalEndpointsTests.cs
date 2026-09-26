using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.MediaProxy.Endpoints;
using ReLiveWP.Services.MediaProxy.Services;
using ReLiveWP.Services.MediaProxy.Utilities;

namespace ReLiveWP.Services.MediaProxy.Tests;

public class InternalEndpointsTests
{
    private const int PublicPort = 10021;
    private const int InternalPort = 5000;

    private static ImagePipelineService CreatePipeline() => new(NullLogger<ImagePipelineService>.Instance);

    private static HttpContext CreateRequest(Stream body)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Body = body;
        return context;
    }

    [Fact]
    public async Task ReturnsJpegForAnImage()
    {
        using var pipeline = CreatePipeline();
        using var source = TestImages.CreateJpeg(400, 300);
        var context = CreateRequest(source);

        var response = await InternalEndpoints.ProcessImageAsync("thumb:96", context, pipeline, CancellationToken.None);

        var file = Assert.IsType<FileContentHttpResult>(response.Result);
        Assert.Equal("image/jpeg", file.ContentType);
        Assert.NotEmpty(file.FileContents.ToArray());
    }

    [Fact]
    public async Task ReturnsUnprocessableWithACodeForJunk()
    {
        using var pipeline = CreatePipeline();
        using var source = new MemoryStream("not an image"u8.ToArray());
        var context = CreateRequest(source);

        var response = await InternalEndpoints.ProcessImageAsync("thumb:96", context, pipeline, CancellationToken.None);

        var refused = Assert.IsType<UnprocessableEntity<MediaPipelineRejection>>(response.Result);
        Assert.Equal(MediaPipelineRejection.Unreadable, refused.Value?.Code);
    }

    [Fact]
    public async Task ReturnsBadRequestForAnUnknownProfileWithoutReadingTheBody()
    {
        using var pipeline = CreatePipeline();
        using var source = TestImages.CreateJpeg(400, 300);
        var context = CreateRequest(source);

        var response = await InternalEndpoints.ProcessImageAsync("thumb:huge", context, pipeline, CancellationToken.None);

        var refused = Assert.IsType<BadRequest<MediaPipelineRejection>>(response.Result);
        Assert.Equal(MediaPipelineRejection.UnknownProfile, refused.Value?.Code);
        Assert.Equal(0, source.Position);
    }

    [Fact]
    public async Task PortFilterPassesRequestsOnTheInternalListener()
    {
        var context = new DefaultHttpContext();
        context.Connection.LocalPort = InternalPort;

        var reached = false;
        var filter = new LocalPortEndpointFilter(InternalPort);
        await filter.InvokeAsync(new DefaultEndpointFilterInvocationContext(context), _ =>
        {
            reached = true;
            return ValueTask.FromResult<object?>(null);
        });

        Assert.True(reached);
    }

    [Fact]
    public async Task PortFilterRefusesThePublicListenerEvenWithAForgedHost()
    {
        var context = new DefaultHttpContext();
        context.Connection.LocalPort = PublicPort;
        context.Request.Host = new HostString("mediaproxy", InternalPort);

        var reached = false;
        var filter = new LocalPortEndpointFilter(InternalPort);
        var result = await filter.InvokeAsync(new DefaultEndpointFilterInvocationContext(context), _ =>
        {
            reached = true;
            return ValueTask.FromResult<object?>(null);
        });

        Assert.False(reached);
        Assert.IsType<NotFound>(result);
    }

    [Theory]
    [InlineData("http://0.0.0.0:5000", 5000)]
    [InlineData("http://127.0.0.3:5000", 5000)]
    [InlineData("http://*:10021", 10021)]
    [InlineData("http://+:10021", 10021)]
    public void ReadsTheListenerPortFromKestrelConfig(string url, int expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Kestrel:Endpoints:Internal:Url"] = url })
            .Build();

        var port = KestrelEndpointPorts.ReadEndpointPort(configuration, KestrelEndpointPorts.InternalEndpoint);

        Assert.Equal(expected, port);
    }

    [Fact]
    public void RefusesToStartWithoutAnInternalListener()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(
            () => KestrelEndpointPorts.ReadEndpointPort(configuration, KestrelEndpointPorts.InternalEndpoint));
    }
}
