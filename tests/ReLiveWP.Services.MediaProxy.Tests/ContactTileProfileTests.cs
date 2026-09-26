using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.ServiceDefaults.Contacts;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.MediaProxy.Models;
using ReLiveWP.Services.MediaProxy.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace ReLiveWP.Services.MediaProxy.Tests;

public class ContactTileProfileTests : IDisposable
{
    private const int Base64Cap = ContactContract.MaxPictureBase64Bytes;

    private readonly ImagePipelineService pipeline = new(NullLogger<ImagePipelineService>.Instance);

    public void Dispose() => pipeline.Dispose();

    // a noisy image is the worst case for jpeg: it will not compress away, so if this fits the cap
    // a real photograph will
    private static byte[] NoisyPng(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var random = new Random(1);

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                    row[x] = new Rgba32((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
            }
        });

        using var output = new MemoryStream();
        image.Save(output, new PngEncoder());
        return output.ToArray();
    }

    // red on top, blue on the bottom, so a crop tells you which end it took
    private static byte[] HalfRedHalfBlue(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                var colour = y < accessor.Height / 2 ? new Rgba32(255, 0, 0) : new Rgba32(0, 0, 255);
                for (var x = 0; x < row.Length; x++)
                    row[x] = colour;
            }
        });

        using var output = new MemoryStream();
        image.Save(output, new PngEncoder());
        return output.ToArray();
    }

    private static long Base64Length(byte[] bytes) => ((long)bytes.Length + 2) / 3 * 4;

    private async Task<byte[]> ProcessAsync(byte[] source, ContactTileProfile profile)
    {
        using var stream = new MemoryStream(source);
        var result = await pipeline.ProcessImageAsync(stream, profile);
        Assert.NotNull(result.Jpeg);
        return result.Jpeg;
    }

    [Fact]
    public async Task ResizesToTheTile()
    {
        var tile = await ProcessAsync(NoisyPng(600, 400), new ContactTileProfile(null, false));

        using var image = Image.Load(tile);
        Assert.Equal(MediaProfiles.ContactTileEdge, image.Width);
        Assert.Equal(MediaProfiles.ContactTileEdge, image.Height);
    }

    // ItemValidationRules rejects an oversized picture, and a rejection aborts the entire
    // SaveChanges batch rather than just that one contact
    [Theory]
    [InlineData(4000, 3000)]
    [InlineData(1024, 1024)]
    [InlineData(200, 200)]
    public async Task OutputAlwaysFitsTheMailboxPictureCap(int width, int height)
    {
        var tile = await ProcessAsync(NoisyPng(width, height), new ContactTileProfile(null, false));

        Assert.True(Base64Length(tile) <= Base64Cap,
            $"{width}x{height} produced {Base64Length(tile)} B base64, over the {Base64Cap} B cap");
    }

    [Fact]
    public async Task APhotoSmallerThanTheTileIsStillSquared()
    {
        var tile = await ProcessAsync(NoisyPng(64, 32), new ContactTileProfile(null, false));

        using var image = Image.Load(tile);
        Assert.Equal(image.Width, image.Height);
    }

    [Fact]
    public void ABottomLeftCropIsMeasuredFromTheFarEdge()
    {
        var frame = ContactTileProfile.FrameCrop(new MediaCrop(28, 21, 679, 679), originIsBottomLeft: true, 727, 727);

        Assert.Equal(new Rectangle(28, 727 - 21 - 679, 679, 679), frame);
    }

    [Fact]
    public void ACropHangingOffTheImageIsClippedToIt()
    {
        var frame = ContactTileProfile.FrameCrop(new MediaCrop(10, 0, 400, 400), originIsBottomLeft: false, 200, 200);

        Assert.Equal(new Rectangle(10, 0, 190, 200), frame);
    }

    // the rect was measured against an image we are not looking at, so framing an edge of this one
    // would be worse than ignoring it
    [Fact]
    public async Task ACropEntirelyOutsideTheImageIsIgnored()
    {
        Assert.Null(ContactTileProfile.FrameCrop(new MediaCrop(900, 900, 100, 100), originIsBottomLeft: false, 200, 200));

        var tile = await ProcessAsync(HalfRedHalfBlue(200, 200), new ContactTileProfile(new MediaCrop(900, 900, 100, 100), false));
        using var image = Image.Load(tile);
        Assert.Equal(MediaProfiles.ContactTileEdge, image.Width);
    }

    [Fact]
    public async Task TheCropSelectsTheRegionTheUserFramed()
    {
        var tile = await ProcessAsync(HalfRedHalfBlue(200, 200), new ContactTileProfile(new MediaCrop(0, 0, 200, 100), true));

        using var image = Image.Load<Rgba32>(tile);
        var pixel = image[MediaProfiles.ContactTileEdge / 2, MediaProfiles.ContactTileEdge / 2];

        Assert.True(pixel.B > pixel.R, $"expected the bottom (blue) half, got {pixel}");
    }
}
