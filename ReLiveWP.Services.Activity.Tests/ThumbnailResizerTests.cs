using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Activity.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace ReLiveWP.Services.Activity.Tests;

// A WebDAV share serves originals only, so these are the sizes the Pictures hub actually receives.
public class ThumbnailResizerTests
{
    private const string Owner = "user-a";

    private static ThumbnailResizer CreateResizer() => new(NullLogger<ThumbnailResizer>.Instance);

    private static MemoryStream CreateJpeg(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var stream = new MemoryStream();
        image.Save(stream, new JpegEncoder());
        stream.Position = 0;
        return stream;
    }

    [Theory]
    [InlineData(800)]
    [InlineData(176)]
    [InlineData(96)]
    public void ProducesRequestedSizeForEachHubThumbnail(int size)
    {
        using var resizer = CreateResizer();
        using var source = CreateJpeg(2400, 1800);

        var thumbnail = resizer.ResizeAsync(Owner, "webdav+abc", size, source).Result;

        Assert.NotNull(thumbnail);
        Assert.Equal("image/jpeg", thumbnail.ContentType);

        using var produced = Image.Load(thumbnail.Data);
        Assert.Equal(size, produced.Width);
        Assert.Equal(size * 3 / 4, produced.Height);
    }

    [Fact]
    public void PreservesAspectRatioOnPortraitSources()
    {
        using var resizer = CreateResizer();
        using var source = CreateJpeg(1000, 2000);

        var thumbnail = resizer.ResizeAsync(Owner, "webdav+portrait", 800, source).Result;

        Assert.NotNull(thumbnail);

        using var produced = Image.Load(thumbnail.Data);
        Assert.Equal(400, produced.Width);
        Assert.Equal(800, produced.Height);
    }

    [Fact]
    public void DoesNotUpscaleSmallSources()
    {
        using var resizer = CreateResizer();
        using var source = CreateJpeg(120, 90);

        var thumbnail = resizer.ResizeAsync(Owner, "webdav+small", 800, source).Result;

        Assert.NotNull(thumbnail);

        using var produced = Image.Load(thumbnail.Data);
        Assert.Equal(120, produced.Width);
        Assert.Equal(90, produced.Height);
    }

    [Fact]
    public void TranscodesPngToJpeg()
    {
        using var resizer = CreateResizer();

        using var image = new Image<Rgba32>(500, 500);
        using var source = new MemoryStream();
        image.Save(source, new PngEncoder());
        source.Position = 0;

        var thumbnail = resizer.ResizeAsync(Owner, "webdav+png", 96, source).Result;

        Assert.NotNull(thumbnail);
        Assert.Equal("image/jpeg", thumbnail.ContentType);
        Assert.Equal(96, Image.Load(thumbnail.Data).Width);
    }

    // an undecodable source must fall back to piping the original rather than erroring the request
    [Fact]
    public void ReturnsNullForContentThatIsNotAnImage()
    {
        using var resizer = CreateResizer();
        using var source = new MemoryStream("this is definitely not a jpeg"u8.ToArray());

        Assert.Null(resizer.ResizeAsync(Owner, "webdav+junk", 800, source).Result);
    }

    [Fact]
    public void SecondCallForTheSameSizeIsServedFromCache()
    {
        using var resizer = CreateResizer();
        using var source = CreateJpeg(1200, 900);

        var first = resizer.ResizeAsync(Owner, "webdav+cached", 176, source).Result;

        // an empty stream would fail to decode, so a result here can only have come from the cache
        using var empty = new MemoryStream();
        var second = resizer.ResizeAsync(Owner, "webdav+cached", 176, empty).Result;

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first.Data, second.Data);
    }

    // a webdav resource ref is base64 of a relative path, so two users who each sync IMG_0001.jpg
    // genuinely share one (see WebDavItemIdTests). the cache is the only thing keeping their photos apart
    [Fact]
    public void TwoOwnersSharingAResourceRefDoNotShareAThumbnail()
    {
        const string shared = "webdav+UmVMaXZlV1Avd21waG90b3MvSU1HXzAwMDEuanBn";

        using var resizer = CreateResizer();

        using var a = CreateJpeg(1200, 900);
        var theirs = resizer.ResizeAsync("user-a", shared, 176, a).Result;

        using var b = CreateJpeg(400, 1000);
        var mine = resizer.ResizeAsync("user-b", shared, 176, b).Result;

        Assert.NotNull(theirs);
        Assert.NotNull(mine);
        Assert.NotEqual(theirs.Data, mine.Data);

        using var produced = Image.Load(mine.Data);
        Assert.Equal(70, produced.Width);
        Assert.Equal(176, produced.Height);
    }
}
