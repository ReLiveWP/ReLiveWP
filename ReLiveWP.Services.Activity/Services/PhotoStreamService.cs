using ReLiveWP.Identity;
using ReLiveWP.ServiceDefaults;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.Activity.Providers;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Services.Activity.Services;

public class PhotoStreamService(PhotoLibraryService library,
                                SocialAlbumsService socialAlbums,
                                SocialAlbumService social,
                                FileViewerService viewer,
                                ThumbnailService thumbnails,
                                MediaProxyClient mediaProxy,
                                IHttpClientFactory httpClientFactory)
{
    public async Task<bool> WriteAsync(HttpContext context, string id, string resourceRef, int maxSize,
                                       CancellationToken ct = default)
    {
        var userId = context.User.Id()!;

        if (socialAlbums.TryResolvePhoto(resourceRef, out var provider, out var externalId, out var mediaId))
        {
            var subjectCid = await viewer.SubjectCidAsync(id, context.User, ct);
            if (!await social.IsServableAsync(provider, externalId, subjectCid, userId, ct))
                return false;

            return await WriteSocialPhotoAsync(context, provider, externalId, mediaId, maxSize, ct);
        }

        using var http = httpClientFactory.CreateClient();

        var resolved = await library.ResolveContentAsync(userId, resourceRef, maxSize, refresh: false, ct);
        if (resolved == null)
            return false;

        var resizeTo = resolved.Value.ResizeTo;
        if (resizeTo > 0 && thumbnails.TryGetCachedThumbnail(userId, resourceRef, resizeTo, out var cached))
        {
            await WriteThumbnailAsync(context, cached, ct);
            return true;
        }

        var location = resolved.Value.Location;
        var forwardRange = resolved.Value.ResizeTo == 0;
        var response = await http.FetchAsync(location, context, ct, forwardRange);
        try
        {
            // the provider urls are short lived, so a stale one looks exactly like a dead item.
            if (!response.IsSuccessStatusCode)
            {
                var refreshed = await library.ResolveContentAsync(userId, resourceRef, maxSize, refresh: true, ct);
                if (refreshed == null)
                    return false;

                response.Dispose();
                resolved = refreshed;
                location = refreshed.Value.Location;
                forwardRange = refreshed.Value.ResizeTo == 0;
                response = await http.FetchAsync(location, context, ct, forwardRange);
            }

            if (resolved.Value.ResizeTo > 0 && response.IsSuccessStatusCode)
            {
                await using var source = await response.Content.ReadAsStreamAsync(ct);
                var sourceLength = response.Content.Headers.ContentLength;
                var thumbnail = await thumbnails.ResizeAsync(userId, resourceRef, resolved.Value.ResizeTo, source,
                                                             sourceLength, ct);

                if (thumbnail != null)
                {
                    await WriteThumbnailAsync(context, thumbnail, ct);
                    return true;
                }

                // a source we can't decode still beats a broken image in the hub
                response.Dispose();
                response = await http.FetchAsync(location, context, ct);
            }

            await response.PipeAsync(location, context, ct);
            return true;
        }
        finally
        {
            response.Dispose();
        }
    }

    private async Task<bool> WriteSocialPhotoAsync(HttpContext context, SocialAlbumProviderBase provider,
                                                   string externalId, string mediaId, int maxSize, CancellationToken ct)
    {
        var size = MediaSizes.ChooseSizeFor(maxSize);
        var source = provider.ResolveMediaSource(externalId, mediaId, size);
        if (source == null)
            return false;

        if (thumbnails.TryGetCachedProxiedImage(source, size, out var cached))
        {
            await WriteThumbnailAsync(context, cached, ct);
            return true;
        }

        using var response = await mediaProxy.FetchImageAsync(source, size, ct);
        if (response == null)
            return false;

        if (!response.IsSuccessStatusCode)
        {
            context.Response.StatusCode = (int)response.StatusCode;
            return true;
        }

        var jpeg = await response.Content.ReadAsByteArrayAsync(ct);
        var image = thumbnails.StoreProxiedImage(source, size, jpeg);
        await WriteThumbnailAsync(context, image, ct);
        return true;
    }

    private static async Task WriteThumbnailAsync(HttpContext context, Thumbnail thumbnail, CancellationToken ct)
    {
        context.Response.ContentType = thumbnail.ContentType;
        context.Response.ContentLength = thumbnail.Data.Length;
        await context.Response.Body.WriteAsync(thumbnail.Data, ct);
    }
}
