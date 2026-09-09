using ReLiveWP.Services.Activity.Models.Web;
using ReLiveWP.Services.Activity.Providers;
using ReLiveWP.Services.Activity.Utilities;

namespace ReLiveWP.Services.Activity.Services;

public class WebAlbumsService(PhotoLibraryService libraries,
                              ConnectionLookupService connections,
                              SocialAlbumsService socialAlbums,
                              SocialAlbumService social,
                              AlbumRenderService renderer)
{
    public async Task<IReadOnlyList<SocialAlbumSummary>> ListOwnAlbumsAsync(FilesUrls urls, string userId,
                                                                            CancellationToken ct = default)
    {
        var albums = new List<SocialAlbumSummary>();

        if (await connections.HasPhotoSyncAsync(ct))
        {
            foreach (var library in await libraries.ListLibrariesAsync(userId, ct))
                albums.Add(renderer.RenderLibrarySummary(urls, userId, library));
        }

        albums.AddRange(await RenderSocialSummariesAsync(await social.OwnedAlbumsAsync(userId, ct), userId, ct));
        return albums;
    }

    public async Task<IReadOnlyList<SocialAlbumSummary>> ListSharedAlbumsAsync(long subjectCid, string userId,
                                                                               CancellationToken ct = default)
    {
        var albums = await social.SharedAlbumsAsync(subjectCid, userId, ct);
        return await RenderSocialSummariesAsync(albums, userId, ct);
    }

    public async Task<SocialAlbumResponse?> OpenOwnAlbumAsync(FilesUrls urls, string userId, string resourceId,
                                                              CancellationToken ct = default)
    {
        if (socialAlbums.TryResolveAlbum(resourceId, out var provider, out var externalId))
            return await OpenSocialAlbumAsync(provider, externalId, resourceId, subjectCid: null, userId, ct);

        var listing = await libraries.ListByFolderAsync(userId, resourceId, ct);
        return listing == null ? null : renderer.RenderLibrary(urls, userId, resourceId, listing);
    }

    public Task<SocialAlbumResponse?> OpenSharedAlbumAsync(long subjectCid, string userId, string resourceId,
                                                           CancellationToken ct = default)
        => socialAlbums.TryResolveAlbum(resourceId, out var provider, out var externalId)
            ? OpenSocialAlbumAsync(provider, externalId, resourceId, subjectCid, userId, ct)
            : Task.FromResult<SocialAlbumResponse?>(null);

    private async Task<SocialAlbumResponse?> OpenSocialAlbumAsync(SocialAlbumProviderBase provider, string externalId,
                                                                  string resourceId, long? subjectCid, string userId,
                                                                  CancellationToken ct)
    {
        if (!await social.IsServableAsync(provider, externalId, subjectCid, userId, ct))
            return null;

        var folder = await social.FolderAsync(provider, externalId, userId, ct);
        await libraries.RememberCoverAsync(userId, resourceId, folder.Photos.FirstOrDefault()?.ResourceRef, ct);

        return renderer.RenderSocialLibrary(resourceId, folder);
    }

    private async Task<List<SocialAlbumSummary>> RenderSocialSummariesAsync(IReadOnlyList<SocialAlbum> albums,
                                                                            string userId, CancellationToken ct)
    {
        var covers = await libraries.CoversAsync(userId, [.. albums.Select(a => a.ResourceId)], ct);

        var summaries = new List<SocialAlbumSummary>();
        foreach (var album in albums)
        {
            var cover = covers.GetValueOrDefault(album.ResourceId) ?? await FirstPhotoRefAsync(album, userId, ct);
            summaries.Add(renderer.RenderSocialSummary(album, cover));
        }

        return summaries;
    }

    // the device only learns a cover by opening the album, the browser lists albums nobody has opened yet
    private async Task<string?> FirstPhotoRefAsync(SocialAlbum album, string userId, CancellationToken ct)
    {
        if (!socialAlbums.TryResolveAlbum(album.ResourceId, out var provider, out var externalId))
            return null;

        var folder = await social.FolderAsync(provider, externalId, userId, ct);
        var cover = folder.Photos.FirstOrDefault()?.ResourceRef;

        await libraries.RememberCoverAsync(userId, album.ResourceId, cover, ct);
        return cover;
    }
}
