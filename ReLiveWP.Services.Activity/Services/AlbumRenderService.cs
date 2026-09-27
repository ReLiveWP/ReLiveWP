using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.Activity.Models.Web;
using ReLiveWP.Services.Activity.Providers;
using ReLiveWP.Services.Activity.Utilities;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Services;

public class AlbumRenderService(MediaTicketService tickets, SocialAlbumsService socialAlbums, MediaProxyUrlSigner mediaProxy)
{
    public const int ThumbnailSize = 320;

    public SocialAlbumSummary RenderLibrarySummary(FilesUrls urls, string userId, Library library)
    {
        var cover = string.IsNullOrEmpty(library.CoverRef) ? null : GetThumbnailUrl(urls, userId, library.CoverRef);
        return new SocialAlbumSummary(library.Id, GetLibraryTitle(library), cover);
    }

    public async Task<SocialAlbumSummary> RenderSocialSummaryAsync(SocialAlbum album, string? coverRef,
                                                                   CancellationToken ct = default)
    {
        var cover = coverRef == null ? null : await GetSocialMediaUrlAsync(coverRef, MediaSize.Thumb, ct);
        return new SocialAlbumSummary(album.ResourceId, album.Title, cover);
    }

    public SocialAlbumResponse RenderLibrary(FilesUrls urls, string userId, string resourceId, PhotoListing listing)
    {
        var photos = new List<SocialAlbumPhoto>();
        foreach (var photo in listing.Photos)
        {
            photos.Add(new SocialAlbumPhoto(
                photo.ResourceRef,
                MediaKinds.IsVideo(photo.MediaType) ? MediaKinds.Video : MediaKinds.Photo,
                GetThumbnailUrl(urls, userId, photo.ResourceRef),
                tickets.SignUrl(urls.ForItemContent(photo.ResourceRef), userId, photo.ResourceRef, 0),
                string.IsNullOrWhiteSpace(photo.Summary) ? null : photo.Summary,
                DateTimeOffset.FromUnixTimeSeconds(photo.CreatedUnix),
                photo.Width,
                photo.Height));
        }

        return new SocialAlbumResponse(resourceId, GetLibraryTitle(listing.Library), photos);
    }

    public async Task<SocialAlbumResponse> RenderSocialLibraryAsync(string resourceId, SocialAlbumFolder folder,
                                                                    CancellationToken ct = default)
    {
        var photos = new List<SocialAlbumPhoto>();
        foreach (var photo in folder.Photos)
        {
            var thumbnail = await GetSocialMediaUrlAsync(photo.ResourceRef, MediaSize.Thumb, ct);
            var fullSize = await GetSocialMediaUrlAsync(photo.ResourceRef, MediaSize.Full, ct);
            if (thumbnail == null || fullSize == null)
                continue;

            var created = new DateTimeOffset(DateTime.SpecifyKind(photo.Created, DateTimeKind.Utc));
            photos.Add(new SocialAlbumPhoto(
                photo.ResourceRef, MediaKinds.Photo, thumbnail, fullSize, photo.Summary, created, photo.Width, photo.Height));
        }

        return new SocialAlbumResponse(resourceId, folder.Title, photos);
    }

    private static string GetLibraryTitle(Library library)
        => string.IsNullOrWhiteSpace(library.Title)
            ? PhotoAlbums.CanonicalNameFor(library.Category) ?? library.Category
            : library.Title;

    private string GetThumbnailUrl(FilesUrls urls, string userId, string resourceRef)
        => tickets.SignUrl(urls.ForItemThumbnail(resourceRef, ThumbnailSize), userId, resourceRef, ThumbnailSize);

    private async Task<string?> GetSocialMediaUrlAsync(string resourceRef, MediaSize size, CancellationToken ct)
    {
        if (!socialAlbums.TryResolvePhoto(resourceRef, out var provider, out var albumKey, out var mediaId))
            return null;

        var source = await provider.ResolveMediaSourceAsync(albumKey, mediaId, size, ct);
        if (source == null)
            return null;

        return mediaProxy.SignUrlOrOriginal(source.AbsoluteUri, size);
    }
}
