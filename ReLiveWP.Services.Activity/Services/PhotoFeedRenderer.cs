using ReLiveWP.Services.Activity.Models.Atom;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Services;

public static class PhotoFeedRenderer
{
    private const long TotalQuota = 25L * 1024 * 1024 * 1024;

    private static readonly int[] ThumbnailSizes = [800, 176, 96];

    public static LiveLibraryFeed Listing(FilesUrls urls) => new()
    {
        Id = urls.Files,
        Updated = DateTime.UtcNow,
        // sample values, not critical
        TotalQuota = TotalQuota,
        MaxFileSize = PhotoUploadReader.MaxFileSize,
        QuotaUsed = 0,
    };

    public static LiveLibraryEntry LibraryEntry(FilesUrls urls, Library library)
    {
        var entry = new LiveLibraryEntry
        {
            Id = urls.Album(library.Category),
            ResourceId = library.Id,
            Type = PhotoAlbums.FolderType,
            CanonicalName = PhotoAlbums.CanonicalNameFor(library.Category)!,
            Category = PhotoAlbums.PhotosCategory,
            SharingLevel = library.SharingLevel,
            EmailKeyword = library.EmailKeyword,
            Title = library.Title,
            Summary = library.Summary,
            Updated = DateTime.UtcNow,
        };

        AddCoverPointer(entry.Thumbnails, library.CoverRef);
        return entry;
    }

    public static LiveLibraryEntry SocialAlbumEntry(FilesUrls urls, SocialAlbum album, string? coverRef)
    {
        var entry = new LiveLibraryEntry
        {
            Id = urls.Folder(album.ResourceId),
            ResourceId = album.ResourceId,
            Type = PhotoAlbums.FolderType,
            Category = PhotoAlbums.PhotosCategory,
            SharingLevel = PhotoAlbums.PublicSharing,
            Title = album.Title,
            Updated = DateTime.UtcNow,
        };

        AddCoverPointer(entry.Thumbnails, coverRef);
        return entry;
    }

    public static LiveAlbumFeed AlbumFeed(FilesUrls urls, string feedId, Library library, IReadOnlyList<PhotoItem> photos)
    {
        var feed = new LiveAlbumFeed
        {
            Id = feedId,
            Title = library.Title,
            Updated = DateTime.UtcNow,
            Type = PhotoAlbums.FolderType,
            ResourceId = library.Id,
            CanonicalName = PhotoAlbums.CanonicalNameFor(library.Category),
            Category = PhotoAlbums.PhotosCategory,
            SharingLevel = library.SharingLevel,
            EmailKeyword = library.EmailKeyword,
            ItemCount = photos.Count,
        };

        AddCoverPointer(feed.Thumbnails, library.CoverRef);

        foreach (var photo in photos)
        {
            var created = DateTimeOffset.FromUnixTimeSeconds(photo.CreatedUnix).UtcDateTime;
            var entry = PhotoEntry(urls, photo.ResourceRef, photo.FileName, photo.Summary, created,
                                   photo.Width, photo.Height, photo.MediaType);

            feed.Entries.Add(entry);
        }

        return feed;
    }

    public static LiveAlbumFeed SocialAlbumFeed(FilesUrls urls, string folderId, string title,
                                                IReadOnlyList<SocialPhoto> photos)
    {
        var feed = new LiveAlbumFeed
        {
            Id = urls.Folder(folderId),
            Title = title,
            Updated = DateTime.UtcNow,
            Type = PhotoAlbums.FolderType,
            ResourceId = folderId,
            Category = PhotoAlbums.PhotosCategory,
            SharingLevel = PhotoAlbums.PublicSharing,
            ItemCount = photos.Count,
        };

        AddCoverPointer(feed.Thumbnails, photos.FirstOrDefault()?.ResourceRef);

        foreach (var photo in photos)
        {
            feed.Entries.Add(PhotoEntry(urls, photo.ResourceRef, photo.FileName, photo.Summary, photo.Created,
                                        photo.Width, photo.Height, MediaKinds.Photo));
        }

        return feed;
    }

    public static LivePhotoInfoFeed PhotoFeed(FilesUrls urls, string resourceRef, string? title)
    {
        var entry = new LiveAlbumItemEntry
        {
            Id = urls.Media(resourceRef),
            ResourceId = resourceRef,
            Type = MediaKinds.PhotoEntry,
            Title = title,
            CommentsEnabled = false,
        };

        AddThumbnails(entry, urls, resourceRef, 0, 0);

        var feed = new LivePhotoInfoFeed { Id = urls.Media(resourceRef), Updated = DateTime.UtcNow };
        feed.Entries.Add(entry);
        return feed;
    }

    public static LivePhotoEntry UploadedEntry(FilesUrls urls, UploadedPhoto photo) => new()
    {
        Id = urls.Media(photo.ResourceRef),
        ResourceId = photo.ResourceRef,
        Type = MediaKinds.EntryType(photo.MediaType),
        Title = photo.FileName,
        Updated = DateTime.UtcNow,
    };

    private static LiveAlbumItemEntry PhotoEntry(FilesUrls urls, string resourceRef, string fileName, string? summary,
                                                 DateTime created, int width, int height, string mediaType)
    {
        var entry = new LiveAlbumItemEntry
        {
            Id = urls.Media(resourceRef),
            ResourceId = resourceRef,
            Type = MediaKinds.EntryType(mediaType),
            Title = fileName,
            Summary = summary,
            CommentsEnabled = false,
            Updated = created,
            Published = created,
        };

        AddThumbnails(entry, urls, resourceRef, width, height);

        if (MediaKinds.IsVideo(mediaType))
            entry.MediaContent = new LiveMediaContent { Url = urls.MediaContent(resourceRef) };

        return entry;
    }

    private static void AddThumbnails(LiveAlbumItemEntry entry, FilesUrls urls, string resourceRef, int width, int height)
    {
        foreach (var size in ThumbnailSizes)
        {
            entry.Thumbnails.Add(new LiveMediaThumbnail
            {
                Url = urls.Thumbnail(resourceRef, size),
                MaxWidth = size,
                Width = width,
                Height = height,
            });
        }
    }

    // the album tile names the photo it wants drawn with and the device fetches that photo itself,
    // so this carries a resourceId and nothing else. an empty one would delete the cover it has.
    private static void AddCoverPointer(List<LiveMediaThumbnail> thumbnails, string? coverRef)
    {
        if (!string.IsNullOrEmpty(coverRef))
            thumbnails.Add(new LiveMediaThumbnail { ResourceId = coverRef });
    }
}
