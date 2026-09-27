using ReLiveWP.ServiceDefaults.Media;

namespace ReLiveWP.Backend.Identity.Services;

public class AvatarProcessingException(string message) : Exception(message);

public readonly record struct AvatarCrop(int X, int Y, int Size);

public record ProcessedAvatar(byte[] Original, byte[] Thumbnail, string ContentType, string Extension);

public class AvatarProcessor(MediaPipelineClient pipeline, ILogger<AvatarProcessor> logger)
{
    public const int MaxSourceBytes = 8 * 1024 * 1024;

    public async Task<ProcessedAvatar> ProcessAsync(byte[] source, AvatarCrop? crop, CancellationToken ct = default)
    {
        if (source.Length == 0)
            throw new AvatarProcessingException("no image data");
        if (source.Length > MaxSourceBytes)
            throw new AvatarProcessingException($"image is {source.Length} B, over the {MaxSourceBytes} B limit");
        if (crop is { Size: <= 0 })
            throw new AvatarProcessingException("crop size must be positive");

        var mediaCrop = crop is { } rect ? new MediaCrop(rect.X, rect.Y, rect.Size, rect.Size) : (MediaCrop?)null;

        var original = await RunProfileAsync(source, MediaProfiles.ForAvatar(mediaCrop), ct);
        var thumbnail = await RunProfileAsync(original, MediaProfiles.ForAvatarThumbnail(), ct);

        logger.LogInformation("processed avatar: {Source} B in, {Original} B original, {Thumb} B thumbnail",
            source.Length, original.Length, thumbnail.Length);

        return new ProcessedAvatar(original, thumbnail, "image/jpeg", ".jpg");
    }

    private async Task<byte[]> RunProfileAsync(byte[] source, string profile, CancellationToken ct)
    {
        using var stream = new MemoryStream(source, writable: false);
        var result = await pipeline.ProcessImageAsync(stream, profile, ct);

        if (result.IsUnavailable)
            throw new MediaPipelineUnavailableException();

        if (result.Jpeg is null)
            throw new AvatarProcessingException(DescribeRejection(result.Rejection!));

        return result.Jpeg;
    }

    internal static string DescribeRejection(MediaPipelineRejection rejection)
    {
        return rejection.Code switch
        {
            MediaPipelineRejection.Unreadable => "could not read the image",
            MediaPipelineRejection.TooManyPixels => $"{rejection.Detail} is too large",
            MediaPipelineRejection.CropOutside => "crop rectangle falls outside the image",
            MediaPipelineRejection.TooLarge => "image is too large",
            _ => $"could not process the image ({rejection.Code})",
        };
    }
}
