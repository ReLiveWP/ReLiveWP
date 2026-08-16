using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.Identity.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace ReLiveWP.Backend.Identity.Tests;

public class AvatarProcessorTests
{
    private static AvatarProcessor NewProcessor() => new(NullLogger<AvatarProcessor>.Instance);

    // left half red, right half blue, so a crop can be told apart from an uncropped frame
    private static byte[] SplitImage(int width, int height, ushort? exifOrientation = null)
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

        if (exifOrientation is { } orientation)
        {
            image.Metadata.ExifProfile = new ExifProfile();
            image.Metadata.ExifProfile.SetValue(ExifTag.Orientation, orientation);
        }

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
    public async Task Thumbnail_stays_under_the_eas_base64_cap()
    {
        var source = SplitImage(2000, 1500);
        var result = await NewProcessor().ProcessAsync(source, null);

        var base64Length = ((long)result.Thumbnail.Length + 2) / 3 * 4;
        Assert.True(base64Length < 48 * 1024, $"thumbnail was {base64Length} B base64");
    }

    [Fact]
    public async Task Output_is_square()
    {
        var result = await NewProcessor().ProcessAsync(SplitImage(400, 200), null);

        using var original = Image.Load(result.Original);
        Assert.Equal(original.Width, original.Height);
    }

    [Fact]
    public async Task No_crop_takes_the_centre()
    {
        // 400x200: a centred square is x 100..300, which straddles the colour boundary at x=200
        var result = await NewProcessor().ProcessAsync(SplitImage(400, 200), null);

        Assert.True(LooksRed(SampleAt(result.Original, 0.15, 0.5)));
        Assert.True(LooksBlue(SampleAt(result.Original, 0.85, 0.5)));
    }

    [Fact]
    public async Task Explicit_crop_selects_the_requested_region()
    {
        var result = await NewProcessor().ProcessAsync(SplitImage(400, 200), new AvatarCrop(0, 0, 200));

        Assert.True(LooksRed(SampleAt(result.Original, 0.15, 0.5)));
        Assert.True(LooksRed(SampleAt(result.Original, 0.85, 0.5)));
    }

    // orientation 6 rotates the frame, moving a different quadrant under the same rect. Cropping the
    // top-left of the *un*oriented frame would still come back red, so the colour tells the two
    // orderings apart.
    [Fact]
    public async Task Crop_is_applied_in_oriented_coordinates()
    {
        var upright = await NewProcessor().ProcessAsync(QuadrantImage(400), new AvatarCrop(0, 0, 200));
        Assert.Equal("red", Name(SampleAt(upright.Original, 0.5, 0.5)));

        var rotated = await NewProcessor().ProcessAsync(QuadrantImage(400, exifOrientation: 6), new AvatarCrop(0, 0, 200));
        Assert.Equal("green", Name(SampleAt(rotated.Original, 0.5, 0.5)));
    }

    [Fact]
    public async Task Crop_outside_the_image_is_rejected()
    {
        var source = SplitImage(400, 200);

        await Assert.ThrowsAsync<AvatarProcessingException>(
            () => NewProcessor().ProcessAsync(source, new AvatarCrop(5000, 5000, 100)));
    }

    [Fact]
    public async Task Crop_with_a_non_positive_size_is_rejected()
    {
        var source = SplitImage(400, 200);

        await Assert.ThrowsAsync<AvatarProcessingException>(
            () => NewProcessor().ProcessAsync(source, new AvatarCrop(0, 0, 0)));
    }

    [Fact]
    public async Task Crop_hanging_off_the_edge_is_clamped()
    {
        var result = await NewProcessor().ProcessAsync(SplitImage(400, 200), new AvatarCrop(-50, -50, 250));

        using var original = Image.Load(result.Original);
        Assert.Equal(original.Width, original.Height);
    }

    [Fact]
    public async Task Oversized_uploads_are_rejected()
    {
        var oversized = new byte[AvatarProcessor.MaxSourceBytes + 1];

        await Assert.ThrowsAsync<AvatarProcessingException>(
            () => NewProcessor().ProcessAsync(oversized, null));
    }

    [Fact]
    public async Task Unreadable_data_is_rejected()
    {
        await Assert.ThrowsAsync<AvatarProcessingException>(
            () => NewProcessor().ProcessAsync("not an image"u8.ToArray(), null));
    }

    [Fact]
    public async Task Empty_data_is_rejected()
    {
        await Assert.ThrowsAsync<AvatarProcessingException>(
            () => NewProcessor().ProcessAsync([], null));
    }
}
