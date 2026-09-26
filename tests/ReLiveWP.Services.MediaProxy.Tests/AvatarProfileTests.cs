using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.MediaProxy.Models;
using ReLiveWP.Services.MediaProxy.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace ReLiveWP.Services.MediaProxy.Tests;

public class AvatarProfileTests : IDisposable
{
    private readonly ImagePipelineService pipeline = new(NullLogger<ImagePipelineService>.Instance);

    public void Dispose() => pipeline.Dispose();

    // left half red, right half blue, so a crop can be told apart from an uncropped frame
    private static byte[] SplitImage(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                    row[x] = x < accessor.Width / 2 ? new Rgba32(255, 0, 0) : new Rgba32(0, 0, 255);
            }
        });

        using var buffer = new MemoryStream();
        image.Save(buffer, new PngEncoder());
        return buffer.ToArray();
    }

    // four distinguishable quadrants, so a rotation is visible in the result rather than symmetric
    private static byte[] QuadrantImage(int size, ushort? exifOrientation = null)
    {
        var colours = new[]
        {
            new Rgba32(255, 0, 0),   // top left
            new Rgba32(0, 0, 255),   // top right
            new Rgba32(0, 255, 0),   // bottom left
            new Rgba32(255, 255, 0), // bottom right
        };

        using var image = new Image<Rgba32>(size, size);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                var bottom = y >= accessor.Height / 2 ? 2 : 0;
                for (var x = 0; x < row.Length; x++)
                    row[x] = colours[bottom + (x >= accessor.Width / 2 ? 1 : 0)];
            }
        });

        if (exifOrientation is { } orientation)
        {
            image.Metadata.ExifProfile = new ExifProfile();
            image.Metadata.ExifProfile.SetValue(ExifTag.Orientation, orientation);
        }

        using var buffer = new MemoryStream();
        image.Save(buffer, new PngEncoder());
        return buffer.ToArray();
    }

    private async Task<byte[]> ProcessAsync(byte[] source, ImageProfile profile)
    {
        using var stream = new MemoryStream(source);
        var result = await pipeline.ProcessImageAsync(stream, profile);
        Assert.NotNull(result.Jpeg);
        return result.Jpeg;
    }

    private static Rgba32 SampleAt(byte[] jpeg, double fx, double fy)
    {
        using var image = Image.Load<Rgba32>(jpeg);
        return image[(int)(image.Width * fx), (int)(image.Height * fy)];
    }

    private static string Name(Rgba32 p) => (p.R > 150, p.G > 150, p.B > 150) switch
    {
        (true, false, false) => "red",
        (false, false, true) => "blue",
        (false, true, false) => "green",
        (true, true, false) => "yellow",
        _ => $"other({p.R},{p.G},{p.B})",
    };

    private static bool LooksRed(Rgba32 p) => p.R > 150 && p.B < 100;
    private static bool LooksBlue(Rgba32 p) => p.B > 150 && p.R < 100;

    [Fact]
    public async Task TheAvatarIsSquareAndAtMostTheMaxEdge()
    {
        var avatar = await ProcessAsync(SplitImage(2000, 1500), new AvatarProfile(null));

        using var image = Image.Load(avatar);
        Assert.Equal(AvatarProfile.MaxEdge, image.Width);
        Assert.Equal(AvatarProfile.MaxEdge, image.Height);
    }

    [Fact]
    public async Task ASmallAvatarIsSquaredWithoutUpscaling()
    {
        var avatar = await ProcessAsync(SplitImage(400, 200), new AvatarProfile(null));

        using var image = Image.Load(avatar);
        Assert.Equal(200, image.Width);
        Assert.Equal(200, image.Height);
    }

    [Fact]
    public async Task NoCropTakesTheCentre()
    {
        // 400x200: a centred square is x 100..300, which straddles the colour boundary at x=200
        var avatar = await ProcessAsync(SplitImage(400, 200), new AvatarProfile(null));

        Assert.True(LooksRed(SampleAt(avatar, 0.15, 0.5)));
        Assert.True(LooksBlue(SampleAt(avatar, 0.85, 0.5)));
    }

    [Fact]
    public async Task AnExplicitCropSelectsTheRequestedRegion()
    {
        var avatar = await ProcessAsync(SplitImage(400, 200), new AvatarProfile(new MediaCrop(0, 0, 200, 200)));

        Assert.True(LooksRed(SampleAt(avatar, 0.15, 0.5)));
        Assert.True(LooksRed(SampleAt(avatar, 0.85, 0.5)));
    }

    // orientation 6 rotates the frame, moving a different quadrant under the same rect. Cropping the
    // top-left of the *un*oriented frame would still come back red, so the colour tells the two
    // orderings apart.
    [Fact]
    public async Task TheCropIsAppliedInOrientedCoordinates()
    {
        var crop = new MediaCrop(0, 0, 200, 200);

        var upright = await ProcessAsync(QuadrantImage(400), new AvatarProfile(crop));
        Assert.Equal("red", Name(SampleAt(upright, 0.5, 0.5)));

        var rotated = await ProcessAsync(QuadrantImage(400, exifOrientation: 6), new AvatarProfile(crop));
        Assert.Equal("green", Name(SampleAt(rotated, 0.5, 0.5)));
    }

    [Fact]
    public async Task ACropOutsideTheImageIsRefused()
    {
        using var stream = new MemoryStream(SplitImage(400, 200));

        var result = await pipeline.ProcessImageAsync(stream, new AvatarProfile(new MediaCrop(5000, 5000, 100, 100)));

        Assert.Null(result.Jpeg);
        Assert.Equal(MediaPipelineRejection.CropOutside, result.Rejection?.Code);
    }

    [Fact]
    public async Task ACropHangingOffTheEdgeIsClamped()
    {
        var avatar = await ProcessAsync(SplitImage(400, 200), new AvatarProfile(new MediaCrop(-50, -50, 250, 250)));

        using var image = Image.Load(avatar);
        Assert.Equal(image.Width, image.Height);
    }

    [Fact]
    public async Task TheThumbnailStaysUnderTheEasBase64Cap()
    {
        var avatar = await ProcessAsync(SplitImage(2000, 1500), new AvatarProfile(null));
        var thumbnail = await ProcessAsync(avatar, new AvatarThumbnailProfile());

        using var image = Image.Load(thumbnail);
        Assert.Equal(AvatarThumbnailProfile.MaxEdge, image.Width);
        Assert.Equal(AvatarThumbnailProfile.MaxEdge, image.Height);

        var base64Length = ((long)thumbnail.Length + 2) / 3 * 4;
        Assert.True(base64Length < 48 * 1024, $"thumbnail was {base64Length} B base64");
    }

    [Fact]
    public async Task TooManyPixelsCarriesTheDimensions()
    {
        using var stream = TestImages.CreatePngClaimingSize(9000, 9000);

        var result = await pipeline.ProcessImageAsync(stream, new AvatarProfile(null));

        Assert.Equal(MediaPipelineRejection.TooManyPixels, result.Rejection?.Code);
        Assert.Equal("9000x9000", result.Rejection?.Detail);
    }
}
