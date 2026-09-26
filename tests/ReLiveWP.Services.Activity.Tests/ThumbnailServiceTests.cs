using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.Activity.Services;

namespace ReLiveWP.Services.Activity.Tests;

public class ThumbnailServiceTests
{
    private const string Owner = "user-a";

    private static ThumbnailService CreateThumbnails(FakeMediaPipeline pipeline)
        => new(pipeline.CreateClient(), NullLogger<ThumbnailService>.Instance);

    [Theory]
    [InlineData(800)]
    [InlineData(176)]
    [InlineData(96)]
    public async Task AsksThePipelineForTheRequestedThumbnail(int size)
    {
        var pipeline = new FakeMediaPipeline();
        using var thumbnails = CreateThumbnails(pipeline);
        using var source = new MemoryStream([1, 2, 3]);

        var thumbnail = await thumbnails.ResizeAsync(Owner, "webdav+abc", size, source, source.Length);

        Assert.NotNull(thumbnail);
        Assert.Equal("image/jpeg", thumbnail.ContentType);
        Assert.Equal([3, 2, 1], thumbnail.Data);

        var call = Assert.Single(pipeline.Calls);
        Assert.Equal($"thumb:{size}", call.Profile);
        Assert.Equal([1, 2, 3], call.Source);
    }

    [Fact]
    public async Task StoresTheResultForTheNextLookup()
    {
        var pipeline = new FakeMediaPipeline();
        using var thumbnails = CreateThumbnails(pipeline);
        using var source = new MemoryStream([1, 2, 3]);

        var produced = await thumbnails.ResizeAsync(Owner, "webdav+cached", 176, source, source.Length);

        Assert.True(thumbnails.TryGetCachedThumbnail(Owner, "webdav+cached", 176, out var cached));
        Assert.Same(produced, cached);
        Assert.False(thumbnails.TryGetCachedThumbnail(Owner, "webdav+cached", 96, out _));
    }

    // a webdav resource ref is base64 of a relative path, so two users who each sync IMG_0001.jpg
    // genuinely share one (see WebDavItemIdTests). the cache is the only thing keeping their photos apart
    [Fact]
    public async Task TwoOwnersSharingAResourceRefDoNotShareAThumbnail()
    {
        const string shared = "webdav+UmVMaXZlV1Avd21waG90b3MvSU1HXzAwMDEuanBn";

        var pipeline = new FakeMediaPipeline();
        using var thumbnails = CreateThumbnails(pipeline);

        using var theirs = new MemoryStream([1, 1, 2]);
        await thumbnails.ResizeAsync("user-a", shared, 176, theirs, theirs.Length);

        Assert.False(thumbnails.TryGetCachedThumbnail("user-b", shared, 176, out _));

        using var mine = new MemoryStream([7, 8, 9]);
        await thumbnails.ResizeAsync("user-b", shared, 176, mine, mine.Length);

        Assert.True(thumbnails.TryGetCachedThumbnail("user-a", shared, 176, out var forA));
        Assert.True(thumbnails.TryGetCachedThumbnail("user-b", shared, 176, out var forB));
        Assert.Equal([2, 1, 1], forA.Data);
        Assert.Equal([9, 8, 7], forB.Data);
    }

    [Fact]
    public async Task ReturnsNullAndCachesNothingWhenThePipelineRefuses()
    {
        var pipeline = new FakeMediaPipeline
        {
            OnProcess = _ => FakeMediaPipeline.RefuseWithCode(MediaPipelineRejection.Unreadable),
        };
        using var thumbnails = CreateThumbnails(pipeline);
        using var source = new MemoryStream([1]);

        Assert.Null(await thumbnails.ResizeAsync(Owner, "webdav+junk", 176, source, source.Length));
        Assert.False(thumbnails.TryGetCachedThumbnail(Owner, "webdav+junk", 176, out _));
    }

    [Fact]
    public async Task ReturnsNullAndCachesNothingWhenThePipelineIsDown()
    {
        var pipeline = new FakeMediaPipeline { OnProcess = _ => throw new HttpRequestException("connection refused") };
        using var thumbnails = CreateThumbnails(pipeline);
        using var source = new MemoryStream([1]);

        Assert.Null(await thumbnails.ResizeAsync(Owner, "webdav+down", 176, source, source.Length));
        Assert.False(thumbnails.TryGetCachedThumbnail(Owner, "webdav+down", 176, out _));
    }
}
