using Microsoft.Extensions.Caching.Memory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ReLiveWP.Services.Activity.Services;

public record Thumbnail(byte[] Data, string ContentType);

// Providers backed by a plain file server hand back originals only, so the sizes the Pictures hub
// asks for have to be produced here.
public class ThumbnailResizer(ILogger<ThumbnailResizer> logger) : IDisposable
{
    private const int MaxSourcePixels = 64_000_000;
    private const int MaxSourceBytes = 64 * 1024 * 1024;
    private const int Quality = 82;

    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    // its own cache rather than the shared one: encoded images dwarf everything else cached in this
    // service, and a SizeLimit on the shared instance would force a Size onto every unrelated entry
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 128L * 1024 * 1024 });

    public void Dispose() => cache.Dispose();

    private static async Task<bool> BufferAsync(Stream source, MemoryStream destination, CancellationToken ct)
    {
        var buffer = new byte[81920];

        while (true)
        {
            var read = await source.ReadAsync(buffer, ct);
            if (read == 0)
                return true;

            if (destination.Length + read > MaxSourceBytes)
                return false;

            await destination.WriteAsync(buffer.AsMemory(0, read), ct);
        }
    }

    public async Task<Thumbnail?> ResizeAsync(string resourceRef, int maxSize, Stream source, CancellationToken ct = default)
    {
        var key = $"thumb:{resourceRef}:{maxSize}";
        if (cache.TryGetValue<Thumbnail>(key, out var cached) && cached != null)
            return cached;

        try
        {
            using var buffered = new MemoryStream();
            if (!await BufferAsync(source, buffered, ct))
            {
                logger.LogWarning("Refusing to resize {ResourceRef}, source exceeds {Limit} bytes", resourceRef, MaxSourceBytes);
                return null;
            }

            buffered.Position = 0;

            // dimensions come from the header, so an oversized image is rejected before its pixels
            // are ever allocated
            var info = await Image.IdentifyAsync(buffered, ct);
            if ((long)info.Width * info.Height > MaxSourcePixels)
            {
                logger.LogWarning("Refusing to resize {ResourceRef}, {Width}x{Height} is too large",
                                  resourceRef, info.Width, info.Height);
                return null;
            }

            buffered.Position = 0;
            using var image = await Image.LoadAsync(buffered, ct);

            // ResizeMode.Max scales up as readily as down, and blowing a small original up to the
            // requested box just ships a blurrier, larger file than the source
            var oversized = image.Width > maxSize || image.Height > maxSize;

            image.Mutate(x =>
            {
                x.AutoOrient();

                if (oversized)
                    x.Resize(new ResizeOptions
                    {
                        Size = new Size(maxSize, maxSize),
                        Mode = ResizeMode.Max,
                        Sampler = KnownResamplers.Lanczos3,
                    });
            });

            using var buffer = new MemoryStream();
            await image.SaveAsJpegAsync(buffer, new JpegEncoder { Quality = Quality }, ct);

            var thumbnail = new Thumbnail(buffer.ToArray(), "image/jpeg");

            cache.Set(key, thumbnail, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = Lifetime,
                Size = thumbnail.Data.Length,
            });

            return thumbnail;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not resize {ResourceRef} to {MaxSize}", resourceRef, maxSize);
            return null;
        }
    }
}
