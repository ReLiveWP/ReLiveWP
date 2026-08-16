using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Services;

public record PhotoListing(Library Library, IReadOnlyList<PhotoItem> Photos);
public record LibraryUpdate(string Type, string SharingLevel, string Title, string Summary, string EmailKeyword);
public readonly record struct ResolvedPhoto(ContentLocation Location, int ResizeTo);

public class PhotoLibraryService(SkyDrive.SkyDriveClient skyDrive, ILogger<PhotoLibraryService> logger)
{
    public async Task<IReadOnlyList<Library>> ListLibrariesAsync(string userId, CancellationToken ct = default)
    {
        var reply = await skyDrive.ListLibrariesAsync(new ListLibrariesRequest { UserId = userId }, cancellationToken: ct);
        return reply.Libraries;
    }

    public Task<PhotoListing?> ListByCategoryAsync(string userId, string category, CancellationToken ct = default)
        => ListAsync(new ListPhotosRequest { UserId = userId, Category = category }, ct);

    public Task<PhotoListing?> ListByFolderAsync(string userId, string folderId, CancellationToken ct = default)
    {
        var request = new ListPhotosRequest { UserId = userId };

        if (PhotoAlbums.TryGetCategory(folderId, out var category))
            request.Category = category;
        else
            request.LibraryId = folderId;

        return ListAsync(request, ct);
    }

    public async Task<Library> CreateOrUpdateAsync(string userId, string category, LibraryUpdate update,
                                                   CancellationToken ct = default)
    {
        var reply = await skyDrive.CreateOrUpdateLibraryAsync(new CreateOrUpdateLibraryRequest
        {
            UserId = userId,
            Type = update.Type,
            Category = category,
            SharingLevel = update.SharingLevel,
            Title = update.Title,
            Summary = update.Summary,
            EmailKeyword = update.EmailKeyword,
        }, cancellationToken: ct);

        return reply.Library;
    }

    public Task ProvisionAsync(string userId, CancellationToken ct = default)
        => skyDrive.ProvisionUserAsync(new ProvisionUserRequest { UserId = userId }, cancellationToken: ct).ResponseAsync;

    public Task ShareAsync(string userId, string category, CancellationToken ct = default)
        => skyDrive.SetAlbumPermissionsAsync(new SetAlbumPermissionsRequest
        {
            UserId = userId,
            Category = category,
            SharingLevel = PhotoAlbums.PublicSharing,
        }, cancellationToken: ct).ResponseAsync;

    public async Task<IReadOnlyDictionary<string, string>> CoversAsync(string userId, IReadOnlyList<string> albumRefs,
                                                                       CancellationToken ct = default)
    {
        if (albumRefs.Count == 0)
            return new Dictionary<string, string>();

        var request = new GetAlbumCoversRequest { UserId = userId };
        request.AlbumRefs.AddRange(albumRefs);

        try
        {
            var reply = await skyDrive.GetAlbumCoversAsync(request, cancellationToken: ct);
            return reply.Covers.ToDictionary(c => c.AlbumRef, c => c.ResourceRef);
        }
        catch (Exception ex)
        {
            // a tile with no picture still opens, a failed listing is retried for hours
            logger.LogWarning(ex, "could not read album covers, listing without them");
            return new Dictionary<string, string>();
        }
    }

    // the album listing can't afford to pull a social feed just to pick a cover, so the cover it
    // shows is whatever the last look at the album's contents left behind
    public async Task RememberCoverAsync(string userId, string albumRef, string? resourceRef, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(resourceRef))
            return;

        try
        {
            await skyDrive.SetAlbumCoverAsync(new SetAlbumCoverRequest
            {
                UserId = userId,
                AlbumRef = albumRef,
                ResourceRef = resourceRef,
            }, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "could not remember the cover for {AlbumRef}", albumRef);
        }
    }

    public async Task<ResolvedPhoto?> ResolveContentAsync(string userId, string resourceRef, int maxSize, bool refresh,
                                                          CancellationToken ct = default)
    {
        var reply = await skyDrive.GetPhotoContentAsync(new GetPhotoContentRequest
        {
            UserId = userId,
            ResourceRef = resourceRef,
            MaxSize = maxSize,
            Refresh = refresh,
        }, cancellationToken: ct);

        if (!reply.Exists)
            return null;

        return new ResolvedPhoto(
            new ContentLocation(reply.Url, reply.Headers, reply.ContentType,
                                string.IsNullOrEmpty(reply.Etag) ? null : reply.Etag),
            reply.ResizeTo);
    }

    private async Task<PhotoListing?> ListAsync(ListPhotosRequest request, CancellationToken ct)
    {
        var reply = await skyDrive.ListPhotosAsync(request, cancellationToken: ct);
        return reply.Exists ? new PhotoListing(reply.Library, reply.Photos) : null;
    }
}
