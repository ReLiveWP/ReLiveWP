using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using ReLiveWP.ServiceDefaults.Media;

namespace ReLiveWP.Services.Activity.Services;

public record Thumbnail(byte[] Data, string ContentType);

// Providers backed by a plain file server hand back originals only, so the sizes the Pictures hub
// asks for have to come from the media pipeline.
public class ThumbnailService(MediaPipelineClient pipeline, ILogger<ThumbnailService> logger) : IDisposable
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    // its own cache rather than the shared one: encoded images dwarf everything else cached in this
    // service, and a SizeLimit on the shared instance would force a Size onto every unrelated entry
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 128L * 1024 * 1024 });

    public void Dispose() => cache.Dispose();

    public bool TryGetCachedThumbnail(string ownerId, string resourceRef, int maxSize,
                                      [NotNullWhen(true)] out Thumbnail? thumbnail)
    {
        var key = CreateCacheKey(ownerId, resourceRef, maxSize);
        return cache.TryGetValue(key, out thumbnail) && thumbnail != null;
    }

    public async Task<Thumbnail?> ResizeAsync(string ownerId, string resourceRef, int maxSize, Stream source,
                                              long? sourceLength, CancellationToken ct = default)
    {
        var profile = MediaProfiles.ForThumbnail(maxSize);
        var result = await pipeline.ProcessImageAsync(source, profile, sourceLength, ct);
        if (result.Jpeg == null)
        {
            logger.LogInformation("No thumbnail for {ResourceRef} at {MaxSize}, serving the original", resourceRef, maxSize);
            return null;
        }

        var key = CreateCacheKey(ownerId, resourceRef, maxSize);
        return StoreImage(key, result.Jpeg);
    }

    public bool TryGetCachedProxiedImage(Uri source, MediaSize size, [NotNullWhen(true)] out Thumbnail? image)
    {
        var key = CreateProxiedCacheKey(source, size);
        return cache.TryGetValue(key, out image) && image != null;
    }

    public Thumbnail StoreProxiedImage(Uri source, MediaSize size, byte[] jpeg)
    {
        var key = CreateProxiedCacheKey(source, size);
        return StoreImage(key, jpeg);
    }

    private Thumbnail StoreImage(string key, byte[] jpeg)
    {
        var image = new Thumbnail(jpeg, MediaProfiles.OutputContentType);

        cache.Set(key, image, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Lifetime,
            Size = image.Data.Length,
        });

        return image;
    }

    // a resourceRef is only unique within an owner, webdav ids are just the file path, so two users
    // with the same filename would otherwise get each other's photo out of the cache
    private static string CreateCacheKey(string ownerId, string resourceRef, int maxSize)
    {
        return $"thumb:{ownerId}:{resourceRef}:{maxSize}";
    }

    // no owner on purpose, only public sources the proxy would sign ever get here
    private static string CreateProxiedCacheKey(Uri source, MediaSize size)
    {
        return $"proxied:{MediaSizes.FormatSize(size)}:{source.AbsoluteUri}";
    }
}
