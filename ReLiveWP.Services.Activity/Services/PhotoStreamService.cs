using ReLiveWP.Identity;
using ReLiveWP.ServiceDefaults;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Services.Activity.Services;

public class PhotoStreamService(PhotoLibraryService library,
                                SocialAlbums socialAlbums,
                                SocialAlbumService social,
                                FilesViewer viewer,
                                ThumbnailResizer thumbnails,
                                IHttpClientFactory httpClientFactory)
{
    public async Task<bool> WriteAsync(HttpContext context, string id, string resourceRef, int maxSize,
                                       CancellationToken ct = default)
    {
        var userId = context.User.Id()!;
        using var http = httpClientFactory.CreateClient();

        if (socialAlbums.TryResolvePhoto(resourceRef, out var provider, out var externalId, out var mediaId))
        {
            var subjectCid = await viewer.SubjectCidAsync(id, userId, ct);
            if (!await social.IsServableAsync(provider, externalId, subjectCid, userId, ct))
                return false;

            var media = provider.GetMediaLocation(externalId, mediaId, maxSize);
            using var mediaResponse = await http.FetchAsync(media, context, ct);

            await mediaResponse.PipeAsync(media, context, ct);
            return true;
        }

        var resolved = await library.ResolveContentAsync(userId, resourceRef, maxSize, refresh: false, ct);
        if (resolved == null)
            return false;

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
                var thumbnail = await thumbnails.ResizeAsync(userId, resourceRef, resolved.Value.ResizeTo, source, ct);

                if (thumbnail != null)
                {
                    context.Response.ContentType = thumbnail.ContentType;
                    context.Response.ContentLength = thumbnail.Data.Length;
                    await context.Response.Body.WriteAsync(thumbnail.Data, ct);
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
}
