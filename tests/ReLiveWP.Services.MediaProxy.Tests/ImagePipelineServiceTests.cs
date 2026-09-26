using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.MediaProxy.Models;
using ReLiveWP.Services.MediaProxy.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Metadata.Profiles.Xmp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ReLiveWP.Services.MediaProxy.Tests;

public class ImagePipelineServiceTests
{
    private const ushort UnsupportedTiffCompression = 34712;

    private sealed record FailingProfile : ImageProfile
    {
        public override void ApplyTransform(IImageProcessingContext context)
        {
            throw new NotSupportedException("this profile always fails");
        }
    }

    private static ImagePipelineService CreatePipeline() => new(NullLogger<ImagePipelineService>.Instance);

    [Theory]
    [InlineData(800)]
    [InlineData(176)]
    [InlineData(96)]
    public async Task ProducesRequestedSizeForEachHubThumbnail(int size)
    {
        using var pipeline = CreatePipeline();
        using var source = TestImages.CreateJpeg(2400, 1800);

        var result = await pipeline.ProcessImageAsync(source, new ThumbnailProfile(size));

        Assert.NotNull(result.Jpeg);
        using var produced = Image.Load(result.Jpeg);
        Assert.Equal(size, produced.Width);
        Assert.Equal(size * 3 / 4, produced.Height);
    }

    [Fact]
    public async Task PreservesAspectRatioOnPortraitSources()
    {
        using var pipeline = CreatePipeline();
        using var source = TestImages.CreateJpeg(1000, 2000);

        var result = await pipeline.ProcessImageAsync(source, new ThumbnailProfile(800));

        Assert.NotNull(result.Jpeg);
        using var produced = Image.Load(result.Jpeg);
        Assert.Equal(400, produced.Width);
        Assert.Equal(800, produced.Height);
    }

    [Fact]
    public async Task DoesNotUpscaleSmallSources()
    {
        using var pipeline = CreatePipeline();
        using var source = TestImages.CreateJpeg(120, 90);

        var result = await pipeline.ProcessImageAsync(source, new ThumbnailProfile(800));

        Assert.NotNull(result.Jpeg);
        using var produced = Image.Load(result.Jpeg);
        Assert.Equal(120, produced.Width);
        Assert.Equal(90, produced.Height);
    }

    [Fact]
    public async Task DoesNotUpscaleSmallPngs()
    {
        using var pipeline = CreatePipeline();
        using var source = TestImages.CreatePng(120, 90, Color.CornflowerBlue);

        var result = await pipeline.ProcessImageAsync(source, new ThumbnailProfile(800));

        Assert.NotNull(result.Jpeg);
        using var produced = Image.Load(result.Jpeg);
        Assert.Equal(120, produced.Width);
        Assert.Equal(90, produced.Height);
    }

    [Fact]
    public async Task ABurstOfPhotosAllComplete()
    {
        using var pipeline = CreatePipeline();

        var requests = Enumerable.Range(0, 24).Select(async _ =>
        {
            using var source = TestImages.CreateJpeg(2400, 1800);
            return await pipeline.ProcessImageAsync(source, new ThumbnailProfile(320));
        });

        var results = await Task.WhenAll(requests);

        Assert.All(results, result => Assert.NotNull(result.Jpeg));
    }

    [Theory]
    [InlineData(100, 100, 1)]
    [InlineData(1000, 1000, 1)]
    [InlineData(1000, 1001, 2)]
    [InlineData(3264, 2448, 8)]
    [InlineData(8000, 8000, 64)]
    public void WeighsDecodesBySourceMegapixels(int width, int height, int expected)
    {
        var info = new ImageInfo(new PixelTypeInfo(32), new Size(width, height), new ImageMetadata());

        Assert.Equal(expected, ImagePipelineService.WeighDecodeInMegapixels(info));
    }

    [Fact]
    public void TheLargestAcceptedSourceFitsTheDecodeBudget()
    {
        var side = (int)Math.Sqrt(ImagePipelineService.MaxSourcePixels);
        var info = new ImageInfo(new PixelTypeInfo(32), new Size(side, side), new ImageMetadata());

        Assert.True(ImagePipelineService.WeighDecodeInMegapixels(info) <= ImagePipelineService.DecodeBudgetMegapixels);
    }

    [Fact]
    public async Task AppliesExifOrientationBeforeResizing()
    {
        using var pipeline = CreatePipeline();

        using var image = new Image<Rgba32>(400, 200);
        image.Metadata.ExifProfile = new ExifProfile();
        image.Metadata.ExifProfile.SetValue(ExifTag.Orientation, (ushort)6);
        using var source = TestImages.EncodeImage(image, new JpegEncoder());

        var result = await pipeline.ProcessImageAsync(source, new ThumbnailProfile(100));

        Assert.NotNull(result.Jpeg);
        using var produced = Image.Load(result.Jpeg);
        Assert.Equal(50, produced.Width);
        Assert.Equal(100, produced.Height);
    }

    [Fact]
    public async Task EmitsBaselineJpegWithSubsampledChroma()
    {
        using var pipeline = CreatePipeline();
        using var source = TestImages.CreatePng(300, 300, Color.CornflowerBlue);

        var result = await pipeline.ProcessImageAsync(source, new ThumbnailProfile(96));

        Assert.NotNull(result.Jpeg);
        var info = Image.Identify(result.Jpeg);
        Assert.Equal(JpegFormat.Instance, info.Metadata.DecodedImageFormat);

        var jpeg = info.Metadata.GetJpegMetadata();
        Assert.False(jpeg.Progressive);
        Assert.Equal(JpegEncodingColor.YCbCrRatio420, jpeg.ColorType);
    }

    [Fact]
    public async Task FlattensTransparencyOntoWhite()
    {
        using var pipeline = CreatePipeline();
        using var source = TestImages.CreatePng(64, 64, Color.Transparent);

        var result = await pipeline.ProcessImageAsync(source, new ThumbnailProfile(96));

        Assert.NotNull(result.Jpeg);
        var centre = TestImages.ReadCentrePixel(result.Jpeg);
        Assert.True(centre is { R: > 245, G: > 245, B: > 245 }, $"expected white, got {centre}");
    }

    [Fact]
    public async Task TakesTheFirstFrameOfAnAnimation()
    {
        using var pipeline = CreatePipeline();
        using var source = TestImages.CreateAnimatedGif(Color.Red, Color.Lime, Color.Blue);

        var result = await pipeline.ProcessImageAsync(source, new ThumbnailProfile(96));

        Assert.NotNull(result.Jpeg);
        var centre = TestImages.ReadCentrePixel(result.Jpeg);
        Assert.True(centre is { R: > 200, G: < 60, B: < 60 }, $"expected red, got {centre}");
    }

    // the pixel cap only sees the canvas, so a long animation inside it must not cost frames times canvas
    [Fact]
    public async Task DecodesOnlyOneFrameOfALongAnimation()
    {
        var colours = Enumerable.Range(0, 40)
            .Select(i => i % 2 == 0 ? Color.Red : Color.Blue)
            .ToArray();
        using var source = TestImages.CreateAnimatedGif(colours);

        using (var everything = Image.Load(source))
            Assert.Equal(40, everything.Frames.Count);

        source.Position = 0;
        using var decoded = await ImagePipelineService.DecodeFirstFrameAsync(source, null, CancellationToken.None);

        Assert.Single(decoded.Frames);
    }

    [Fact]
    public async Task StripsExifAndXmp()
    {
        using var pipeline = CreatePipeline();

        using var image = new Image<Rgba32>(200, 200);
        image.Metadata.ExifProfile = new ExifProfile();
        image.Metadata.ExifProfile.SetValue(ExifTag.Artist, "someone");
        image.Metadata.XmpProfile = new XmpProfile(Encoding.UTF8.GetBytes(
            "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"/></x:xmpmeta>"));
        using var source = TestImages.EncodeImage(image, new JpegEncoder());

        var original = Image.Identify(source.ToArray());
        Assert.NotNull(original.Metadata.ExifProfile);
        Assert.NotNull(original.Metadata.XmpProfile);

        var result = await pipeline.ProcessImageAsync(source, new ThumbnailProfile(96));

        Assert.NotNull(result.Jpeg);
        var produced = Image.Identify(result.Jpeg);
        Assert.Null(produced.Metadata.ExifProfile);
        Assert.Null(produced.Metadata.XmpProfile);
        Assert.Null(produced.Metadata.IptcProfile);
        Assert.Null(produced.Metadata.IccProfile);
    }

    [Fact]
    public async Task RefusesContentThatIsNotAnImage()
    {
        using var pipeline = CreatePipeline();
        using var source = new MemoryStream("this is definitely not a jpeg"u8.ToArray());

        var result = await pipeline.ProcessImageAsync(source, new ThumbnailProfile(96));

        Assert.Null(result.Jpeg);
        Assert.Equal(MediaPipelineRejection.Unreadable, result.Rejection?.Code);
    }

    [Fact]
    public async Task RefusesAnImageTheDecoderDoesNotSupport()
    {
        using var pipeline = CreatePipeline();
        using var source = TestImages.CreateTiffWithCompression(UnsupportedTiffCompression);

        var result = await pipeline.ProcessImageAsync(source, new ThumbnailProfile(96));

        Assert.Null(result.Jpeg);
        Assert.Equal(MediaPipelineRejection.Unreadable, result.Rejection?.Code);
    }

    [Fact]
    public async Task RefusesWhenTheTransformFails()
    {
        using var pipeline = CreatePipeline();
        using var source = TestImages.CreateJpeg(64, 64);

        var result = await pipeline.ProcessImageAsync(source, new FailingProfile());

        Assert.Null(result.Jpeg);
        Assert.Equal(MediaPipelineRejection.Unreadable, result.Rejection?.Code);
    }

    [Fact]
    public async Task RefusesAnEmptySource()
    {
        using var pipeline = CreatePipeline();
        using var source = new MemoryStream();

        var result = await pipeline.ProcessImageAsync(source, new ThumbnailProfile(96));

        Assert.Equal(MediaPipelineRejection.Unreadable, result.Rejection?.Code);
    }

    [Fact]
    public async Task RefusesAnOversizedSourceWithoutReadingItAll()
    {
        using var pipeline = CreatePipeline();
        using var source = new ZeroFilledStream(ImagePipelineService.MaxSourceBytes * 2L);

        var result = await pipeline.ProcessImageAsync(source, new ThumbnailProfile(96));

        Assert.Equal(MediaPipelineRejection.TooLarge, result.Rejection?.Code);
        Assert.True(source.Position < ImagePipelineService.MaxSourceBytes + 1024 * 1024);
    }

    [Fact]
    public async Task RefusesADeclaredLengthOverTheCapWithoutReadingIt()
    {
        using var pipeline = CreatePipeline();
        using var source = new ZeroFilledStream(ImagePipelineService.MaxSourceBytes + 1L);

        var result = await pipeline.ProcessImageAsync(source, new ThumbnailProfile(96), ImagePipelineService.MaxSourceBytes,
                                                      ImagePipelineService.MaxSourceBytes + 1L);

        Assert.Equal(MediaPipelineRejection.TooLarge, result.Rejection?.Code);
        Assert.Equal(0, source.Position);
    }

    [Theory]
    [InlineData(null, 64)]
    [InlineData(0L, 1)]
    [InlineData(1L, 1)]
    [InlineData(1024L * 1024, 1)]
    [InlineData(1024L * 1024 + 1, 2)]
    [InlineData(5L * 1024 * 1024, 5)]
    public void WeighsReceivesByDeclaredLengthOrTheCap(long? declared, int expected)
    {
        Assert.Equal(expected, ImagePipelineService.WeighReceiveInMegabytes(declared, ImagePipelineService.MaxSourceBytes));
    }

    [Fact]
    public async Task ABurstOfUndeclaredSourcesLargerThanTheReceiveBudgetAllComplete()
    {
        using var pipeline = CreatePipeline();
        var overBudget = ImagePipelineService.ReceiveBudgetMegabytes / 64 * 2;

        var requests = Enumerable.Range(0, overBudget).Select(async _ =>
        {
            using var source = TestImages.CreateJpeg(200, 150);
            return await pipeline.ProcessImageAsync(source, new ThumbnailProfile(96));
        });

        var results = await Task.WhenAll(requests);

        Assert.All(results, result => Assert.NotNull(result.Jpeg));
    }

    [Fact]
    public async Task RefusesTooManyPixelsFromTheHeaderAlone()
    {
        using var pipeline = CreatePipeline();
        using var source = TestImages.CreatePngClaimingSize(9000, 9000);

        var result = await pipeline.ProcessImageAsync(source, new ThumbnailProfile(96));

        Assert.Equal(MediaPipelineRejection.TooManyPixels, result.Rejection?.Code);
    }
}
