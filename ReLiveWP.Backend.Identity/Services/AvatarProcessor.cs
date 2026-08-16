using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ReLiveWP.Backend.Identity.Services;

public class AvatarProcessingException(string message) : Exception(message);

public readonly record struct AvatarCrop(int X, int Y, int Size);

public record ProcessedAvatar(byte[] Original, byte[] Thumbnail, string ContentType, string Extension);

public class AvatarProcessor(ILogger<AvatarProcessor> logger)
{
    public const int MaxSourceBytes = 8 * 1024 * 1024;

    private const int MaxSourcePixels = 64_000_000;
    private const int OriginalSize = 512;
    private const int ThumbnailSize = 128;
    private const int Quality = 82;

    public async Task<ProcessedAvatar> ProcessAsync(byte[] source, AvatarCrop? crop, CancellationToken ct = default)
    {
        if (source.Length == 0)
            throw new AvatarProcessingException("no image data");
        if (source.Length > MaxSourceBytes)
            throw new AvatarProcessingException($"image is {source.Length} B, over the {MaxSourceBytes} B limit");

        using var buffered = new MemoryStream(source, writable: false);

        ImageInfo info;
        try
        {
            info = await Image.IdentifyAsync(buffered, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new AvatarProcessingException("could not read the image");
        }

        if ((long)info.Width * info.Height > MaxSourcePixels)
            throw new AvatarProcessingException($"{info.Width}x{info.Height} is too large");

        buffered.Position = 0;
        using var image = await Image.LoadAsync(buffered, ct);

        // before the crop, always: the browser previews with image-orientation: from-image, so the
        // rect the user drew is in oriented coordinates
        image.Mutate(x => x.AutoOrient());

        if (crop is { } rect)
            image.Mutate(x => x.Crop(ResolveCrop(rect, image.Width, image.Height)));

        // squares the result if the crop was clamped, or if there was no crop at all
        var side = Math.Min(OriginalSize, Math.Min(image.Width, image.Height));
        image.Mutate(x => x
            .Resize(new ResizeOptions
            {
                Size = new Size(side, side),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center,
                Sampler = KnownResamplers.Lanczos3,
            })
            .BackgroundColor(Color.White));

        var original = await EncodeAsync(image, ct);

        var thumbSide = Math.Min(ThumbnailSize, side);
        using var thumbnail = image.Clone(x => x.Resize(thumbSide, thumbSide, KnownResamplers.Lanczos3));
        var thumb = await EncodeAsync(thumbnail, ct);

        logger.LogInformation("processed avatar: {Source} B in, {Original} B original, {Thumb} B thumbnail",
            source.Length, original.Length, thumb.Length);

        return new ProcessedAvatar(original, thumb, "image/jpeg", ".jpg");
    }

    private static Rectangle ResolveCrop(AvatarCrop crop, int width, int height)
    {
        if (crop.Size <= 0)
            throw new AvatarProcessingException("crop size must be positive");

        var requested = new Rectangle(crop.X, crop.Y, crop.Size, crop.Size);
        var clamped = Rectangle.Intersect(requested, new Rectangle(0, 0, width, height));
        if (clamped.Width <= 0 || clamped.Height <= 0)
            throw new AvatarProcessingException("crop rectangle falls outside the image");

        return clamped;
    }

    private static async Task<byte[]> EncodeAsync(Image image, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await image.SaveAsJpegAsync(buffer, new JpegEncoder { Quality = Quality }, ct);
        return buffer.ToArray();
    }
}
