using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.ClearingHouse.Services.ContactSync;
using ReLiveWP.Services.Grpc.Mailbox;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ReLiveWP.Backend.ClearingHouse.Tests;

public class ContactPhotoServiceTests
{
    private const int Base64Cap = 48 * 1024;

    private static ContactPhotoService NewService()
    {
        var factory = new NoopHttpClientFactory();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Endpoints:ConnectedServices:Proxy"] = "http://connectedservices",
            })
            .Build();

        return new(factory, new ConnectedServicesProxy(factory, config),
            NullLogger<ContactPhotoService>.Instance);
    }

    private static RemoteContact WithPhoto(byte[]? data, string? url = null) =>
        new("x", new ContactItem(), PhotoUrl: url, PhotoData: data);

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

    private static long Base64Length(byte[] bytes) => ((long)bytes.Length + 2) / 3 * 4;

    [Fact]
    public async Task Resizes_an_inline_photo_to_the_tile_size()
    {
        var result = await NewService().ResolveAsync(WithPhoto(NoisyPng(600, 400)));

        Assert.NotNull(result);

        using var image = Image.Load(result);
        Assert.Equal(ContactPhotoService.TileSize, image.Width);
        Assert.Equal(ContactPhotoService.TileSize, image.Height);
    }

    // ItemValidationRules rejects an oversized picture, and a rejection aborts the entire
    // SaveChanges batch rather than just that one contact
    [Theory]
    [InlineData(4000, 3000)]
    [InlineData(1024, 1024)]
    [InlineData(200, 200)]
    public async Task Output_always_fits_the_mailbox_picture_cap(int width, int height)
    {
        var result = await NewService().ResolveAsync(WithPhoto(NoisyPng(width, height)));

        Assert.NotNull(result);
        Assert.True(Base64Length(result) <= Base64Cap,
            $"{width}x{height} produced {Base64Length(result)} B base64, over the {Base64Cap} B cap");
    }

    [Fact]
    public async Task A_photo_smaller_than_the_tile_is_still_squared()
    {
        var result = await NewService().ResolveAsync(WithPhoto(NoisyPng(64, 32)));

        Assert.NotNull(result);
        using var image = Image.Load(result);
        Assert.Equal(image.Width, image.Height);
    }

    [Fact]
    public async Task Unreadable_data_yields_no_photo_rather_than_throwing()
    {
        Assert.Null(await NewService().ResolveAsync(WithPhoto([1, 2, 3, 4, 5])));
    }

    [Fact]
    public async Task A_contact_with_no_photo_yields_null()
    {
        Assert.Null(await NewService().ResolveAsync(WithPhoto(null)));
    }

    // these urls arrive inside a provider response, so anything off the provider's own cdn is
    // refused rather than fetched
    [Theory]
    [InlineData("http://lh3.googleusercontent.com/a/x")]
    [InlineData("https://evil.example/steal")]
    [InlineData("https://googleusercontent.com.evil.example/x")]
    [InlineData("https://127.0.0.1/x")]
    [InlineData("file:///etc/passwd")]
    public async Task Refuses_photo_urls_off_the_allowlist(string url)
    {
        Assert.Null(await NewService().ResolveAsync(WithPhoto(null, url)));
    }

    [Fact]
    public void A_bottom_left_crop_is_measured_from_the_far_edge()
    {
        var frame = ContactPhotoService.Frame(new PhotoCrop(28, 21, 679, 679, OriginIsBottomLeft: true), 727, 727);

        Assert.Equal(new Rectangle(28, 727 - 21 - 679, 679, 679), frame);
    }

    [Fact]
    public void A_crop_hanging_off_the_image_is_clipped_to_it()
    {
        var frame = ContactPhotoService.Frame(new PhotoCrop(10, 0, 400, 400, OriginIsBottomLeft: false), 200, 200);

        Assert.Equal(new Rectangle(10, 0, 190, 200), frame);
    }

    // the rect was measured against an image we are not looking at, so framing an edge of this one
    // would be worse than ignoring it
    [Fact]
    public void A_crop_entirely_outside_the_image_is_discarded()
    {
        Assert.Null(ContactPhotoService.Frame(new PhotoCrop(900, 900, 100, 100, OriginIsBottomLeft: false), 200, 200));
    }

    [Fact]
    public async Task The_crop_selects_the_region_the_user_framed()
    {
        var source = HalfRedHalfBlue(200, 200);

        var cropped = await NewService().ResolveAsync(
            new RemoteContact("x", new ContactItem(), PhotoData: source,
                PhotoCrop: new PhotoCrop(0, 0, 200, 100, OriginIsBottomLeft: true)));

        Assert.NotNull(cropped);

        using var image = Image.Load<Rgba32>(cropped);
        var pixel = image[ContactPhotoService.TileSize / 2, ContactPhotoService.TileSize / 2];

        Assert.True(pixel.B > pixel.R, $"expected the bottom (blue) half, got {pixel}");
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

    private sealed class NoopHttpClientFactory : System.Net.Http.IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            throw new InvalidOperationException("no fetch should have been attempted");
    }
}
