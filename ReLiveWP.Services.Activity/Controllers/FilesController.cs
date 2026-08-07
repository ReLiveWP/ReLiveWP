using System.Xml.Linq;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using ReLiveWP.Identity;
using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Activity.Models.Atom;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Grpc;
using ContentRangeHeaderValue = System.Net.Http.Headers.ContentRangeHeaderValue;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Services.Activity.Controllers;

[Controller]
[Authorize]
[Produces("application/atom+xml")]
public class FilesController(SkyDrive.SkyDriveClient skyDrive,
                             ConnectedServices.ConnectedServicesClient connectedServices,
                             SocialAlbumProvider socialAlbums,
                             IHttpClientFactory httpClientFactory,
                             ILogger<FilesController> logger) : Controller
{
    private const string AlbumRoute = "/Users({id})/Files/{album:regex(^(wmphotos|mobilephotos|twitterphotos)$)}";
    private const string PhotosCategory = "Photos";
    private const string FolderType = "Folder";
    private const string PhotoType = "photo";
    private const string VideoType = "video";

    private const string PublicSharedLevel = "publicshared";

    private static readonly int[] ThumbnailSizes = [800, 176, 96];

    private const uint PhotoSyncCapability = 0x10;

    private const long TotalQuota = 25L * 1024 * 1024 * 1024;
    private const long MaxFileSize = 100L * 1024 * 1024;
    private const int SpoolMemoryThreshold = 1024 * 1024;

    private static readonly Dictionary<string, string> WellKnownFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["WMPhotos"] = "wmphotos",
        ["MobilePhotos"] = "mobilephotos",
        ["TwitterPhotos"] = "twitterphotos",
    };

    private static readonly Dictionary<string, string> CanonicalNames = new()
    {
        ["wmphotos"] = "WMPhotos",
        ["mobilephotos"] = "MobilePhotos",
        ["twitterphotos"] = "TwitterPhotos",
    };

    private static string? CanonicalNameFor(string category)
        => CanonicalNames.TryGetValue(category, out var name) ? name : null;

    [HttpGet]
    [Route("/Users({id})/Files")]
    public async Task<ActionResult> GetFiles(string id)
    {
        var reply = await skyDrive.ListLibrariesAsync(new ListLibrariesRequest { UserId = User.Id() });

        var feed = new LiveLibraryFeed
        {
            Id = $"{Request.Scheme}://{Request.Host}/Users({id})/Files",
            Updated = DateTime.UtcNow,
            // sample values, not critical
            TotalQuota = TotalQuota,
            MaxFileSize = MaxFileSize,
            QuotaUsed = 0,
        };

        if (await HasPhotoSyncProviderAsync())
        {
            foreach (var library in reply.Libraries)
                feed.Entries.Add(ToEntry(id, library.Category, library));
        }

        foreach (var album in await GetSocialAlbumsAsync())
        {
            feed.Entries.Add(new LiveLibraryEntry
            {
                Id = $"{Request.Scheme}://{Request.Host}/Users({id})/Files/folders('{album.ResourceId}')",
                ResourceId = album.ResourceId,
                Type = FolderType,
                Category = PhotosCategory,
                SharingLevel = "publicshared",
                Title = album.Title,
                Updated = DateTime.UtcNow,
            });
        }

        return Ok(feed);
    }

    private async Task<bool> HasPhotoSyncProviderAsync()
    {
        var call = connectedServices.GetConnections(
            new ConnectionsRequest { Capabilities = PhotoSyncCapability }, cancellationToken: HttpContext.RequestAborted);

        await foreach (var _ in call.ResponseStream.ReadAllAsync(HttpContext.RequestAborted))
            return true;

        return false;
    }

    private async Task<IReadOnlyList<SocialAlbum>> GetSocialAlbumsAsync()
    {
        var connections = new List<Connection>();
        var call = connectedServices.GetConnections(new ConnectionsRequest(), cancellationToken: HttpContext.RequestAborted);
        await foreach (var connection in call.ResponseStream.ReadAllAsync(HttpContext.RequestAborted))
            connections.Add(connection);

        return await socialAlbums.GetAlbumsAsync(User.Id()!, connections, HttpContext.RequestAborted);
    }

    [HttpGet]
    [Route(AlbumRoute)]
    public async Task<ActionResult> GetAlbum(string id, string album)
    {
        var reply = await skyDrive.ListPhotosAsync(new ListPhotosRequest
        {
            UserId = User.Id(),
            Category = album,
        });

        if (!reply.Exists)
            return NotFound();

        var baseUri = $"{Request.Scheme}://{Request.Host}";
        var feed = BuildAlbumFeed(reply.Library, $"{baseUri}/Users({id})/Files/{album}", reply.Photos.Count);

        foreach (var photo in reply.Photos)
            AddPhotoEntry(feed, baseUri, id, photo);

        return Ok(feed);
    }

    private static LiveAlbumFeed BuildAlbumFeed(Library library, string feedId, int itemCount) => new()
    {
        Id = feedId,
        Title = library.Title,
        Updated = DateTime.UtcNow,
        Type = FolderType,
        ResourceId = library.Id,
        CanonicalName = CanonicalNameFor(library.Category),
        Category = PhotosCategory,
        SharingLevel = library.SharingLevel,
        EmailKeyword = library.EmailKeyword,
        ItemCount = itemCount,
    };

    private static void AddPhotoEntry(LiveAlbumFeed feed, string baseUri, string id, PhotoItem photo)
    {
        var isVideo = photo.MediaType == VideoType;
        var created = DateTimeOffset.FromUnixTimeSeconds(photo.CreatedUnix).UtcDateTime;
        var mediaBase = $"{baseUri}/Users({id})/Files/files('{photo.ResourceRef}')";

        var entry = new LiveAlbumItemEntry
        {
            Id = mediaBase,
            ResourceId = photo.ResourceRef,
            Type = isVideo ? "Video" : "Photo",
            Title = photo.FileName,
            Summary = photo.Summary,
            CommentsEnabled = false,
            Updated = created,
            Published = created,
        };

        foreach (var size in ThumbnailSizes)
        {
            entry.Thumbnails.Add(new LiveMediaThumbnail
            {
                Url = $"{mediaBase}/thumbnail/{size}",
                MaxWidth = size,
                Width = photo.Width,
                Height = photo.Height,
            });
        }

        if (isVideo)
            entry.MediaContent = new LiveMediaContent { Url = $"{mediaBase}/media" };

        feed.Entries.Add(entry);
    }

    [HttpPut]
    [Route(AlbumRoute)]
    public async Task<ActionResult> PutAlbum(string id, string album, [FromBody] LiveLibraryEntry entry)
    {
        var reply = await skyDrive.CreateOrUpdateLibraryAsync(new CreateOrUpdateLibraryRequest
        {
            UserId = User.Id(),
            Type = entry.Type ?? "Library",
            Category = album,
            SharingLevel = entry.SharingLevel ?? "private",
            Title = entry.Title?.Value ?? "",
            Summary = entry.Summary?.Value ?? "",
            EmailKeyword = entry.EmailKeyword ?? "",
        });

        return Ok(ToEntry(id, album, reply.Library));
    }

    [HttpPost]
    [Route(AlbumRoute)]
    public async Task<ActionResult> UploadPhoto(string id, string album)
    {
        if (!MediaTypeHeaderValue.TryParse(Request.ContentType, out var mediaType) ||
            !mediaType.MediaType.Equals("multipart/related", StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status415UnsupportedMediaType);

        var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
        if (string.IsNullOrEmpty(boundary))
            return BadRequest();

        var userId = User.Id();
        var reader = new MultipartReader(boundary, Request.Body);

        var ct = HttpContext.RequestAborted;

        string? fileName = null;
        string? summary = null;
        string? liveType = null;
        var resolveNameConflict = false;
        var suppressNotification = false;
        string? imageContentType = null;

        FileBufferingReadStream? spool = null;

        try
        {
            for (var section = await reader.ReadNextSectionAsync(ct);
                 section != null;
                 section = await reader.ReadNextSectionAsync(ct))
            {
                var sectionType = section.ContentType ?? "";
                if (sectionType.Contains("atom+xml", StringComparison.OrdinalIgnoreCase))
                {
                    var doc = await XDocument.LoadAsync(section.Body, LoadOptions.None, ct);
                    XNamespace atom = Constants.Atom_Namespace;
                    XNamespace live = Constants.Live_Namespace;
                    fileName = doc.Root?.Element(atom + "title")?.Value;
                    summary = doc.Root?.Element(atom + "summary")?.Value;
                    liveType = doc.Root?.Element(live + "type")?.Value;
                    resolveNameConflict = ParseLiveBool(doc.Root?.Element(live + "ResolveNameConflict")?.Value);
                    suppressNotification = ParseLiveBool(doc.Root?.Element(live + "SuppressNotification")?.Value);
                }
                else
                {
                    spool = new FileBufferingReadStream(section.Body, SpoolMemoryThreshold, MaxFileSize, Path.GetTempPath());
                    await spool.DrainAsync(ct);

                    imageContentType = NormaliseContentType(sectionType, liveType);
                }
            }

            if (spool is null || spool.Length == 0)
                return BadRequest();

            liveType = string.Equals(liveType, VideoType, StringComparison.OrdinalIgnoreCase) ? VideoType : PhotoType;
            fileName = string.IsNullOrWhiteSpace(fileName)
                ? $"{Guid.NewGuid():N}{(liveType == VideoType ? ".mp4" : ".jpg")}"
                : fileName;

            return await SyncPhotoAsync(id, spool, new PhotoUploadMetadata
            {
                UserId = userId,
                Category = album,
                FileName = fileName,
                ContentType = imageContentType ?? NormaliseContentType("", liveType),
                Summary = summary ?? "",
                MediaType = liveType,
                ResolveNameConflict = resolveNameConflict,
                SuppressNotification = suppressNotification,
            }, ct);
        }
        catch (IOException)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }
        finally
        {
            if (spool != null)
                await spool.DisposeAsync();
        }
    }

    private async Task<ActionResult> SyncPhotoAsync(string id, Stream spool, PhotoUploadMetadata metadata, CancellationToken ct)
    {
        BeginPhotoUploadReply plan;
        try
        {
            plan = await skyDrive.BeginPhotoUploadAsync(new BeginPhotoUploadRequest
            {
                Metadata = metadata,
                ContentLength = spool.Length,
            }, cancellationToken: ct);
        }
        catch (RpcException ex) when (ex.StatusCode is global::Grpc.Core.StatusCode.FailedPrecondition
                                                     or global::Grpc.Core.StatusCode.InvalidArgument)
        {
            return BadRequest();
        }

        var complete = new CompletePhotoUploadRequest { Metadata = metadata };
        foreach (var target in plan.Targets)
            complete.Outcomes.Add(await SendUploadAsync(target, spool, ct));

        PhotoReply created;
        try
        {
            created = await skyDrive.CompletePhotoUploadAsync(complete, cancellationToken: ct);
        }
        catch (RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.FailedPrecondition)
        {
            return BadRequest();
        }

        return Ok(new LivePhotoEntry
        {
            Id = $"{Request.Scheme}://{Request.Host}/Users({id})/Files/files('{created.ResourceRef}')",
            ResourceId = created.ResourceRef,
            Type = created.MediaType == VideoType ? "Video" : "Photo",
            Title = created.FileName,
            Updated = DateTime.UtcNow,
        });
    }

    // a failed target is recorded rather than thrown so one broken provider can't sink an upload the
    // others accepted; CompletePhotoUpload decides whether anything usable came back.
    private async Task<UploadOutcome> SendUploadAsync(UploadTarget target, Stream spool, CancellationToken ct)
    {
        var outcome = new UploadOutcome { Service = target.Service, ConnectionId = target.ConnectionId };

        try
        {
            using var http = httpClientFactory.CreateClient();

            var total = spool.Length;
            var fragment = target.FragmentSize > 0 ? target.FragmentSize : total;

            for (var offset = 0L; offset < total; offset += fragment)
            {
                var length = Math.Min(fragment, total - offset);

                spool.Seek(offset, SeekOrigin.Begin);

                using var request = new HttpRequestMessage(new HttpMethod(target.Method), target.Url)
                {
                    Content = new StreamContent(new WindowStream(spool, length))
                };

                foreach (var (name, value) in target.Headers)
                {
                    if (!request.Headers.TryAddWithoutValidation(name, value))
                        request.Content.Headers.TryAddWithoutValidation(name, value);
                }

                request.Content.Headers.ContentLength = length;

                if (target.FragmentSize > 0)
                    request.Content.Headers.ContentRange = new ContentRangeHeaderValue(offset, offset + length - 1, total);

                using var response = await http.SendAsync(request, ct);

                outcome.StatusCode = (int)response.StatusCode;
                outcome.ResponseBody = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Upload to {Service} ({ConnectionId}) failed", target.Service, target.ConnectionId);
            outcome.StatusCode = 0;
            outcome.ResponseBody = "";
        }

        return outcome;
    }

    private static bool ParseLiveBool(string? value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";

    private static string NormaliseContentType(string sectionType, string? mediaType)
    {
        var isVideo = string.Equals(mediaType, VideoType, StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(sectionType) || !sectionType.Contains('/'))
            return isVideo ? "video/mp4" : "image/jpeg";

        return sectionType;
    }

    [HttpGet]
    [Route("/Users({id})/Files/folders('{folderId}')")]
    public async Task<ActionResult> GetFolder(string id, string folderId)
    {
        if (SocialAlbumProvider.TryParseAlbumId(folderId, out var did))
            return await GetSocialFolderAsync(id, folderId, did);

        var request = new ListPhotosRequest { UserId = User.Id() };

        if (WellKnownFolders.TryGetValue(folderId, out var category))
            request.Category = category;
        else
            request.LibraryId = folderId;

        var reply = await skyDrive.ListPhotosAsync(request);

        if (!reply.Exists)
            return NotFound();

        var baseUri = $"{Request.Scheme}://{Request.Host}";
        var feed = BuildAlbumFeed(reply.Library, $"{baseUri}/Users({id})/Files/folders('{folderId}')", reply.Photos.Count);

        foreach (var photo in reply.Photos)
            AddPhotoEntry(feed, baseUri, id, photo);

        return Ok(feed);
    }

    private async Task<ActionResult> GetSocialFolderAsync(string id, string folderId, string did)
    {
        var connections = new List<Connection>();
        var call = connectedServices.GetConnections(new ConnectionsRequest(), cancellationToken: HttpContext.RequestAborted);
        await foreach (var connection in call.ResponseStream.ReadAllAsync(HttpContext.RequestAborted))
            connections.Add(connection);

        var owned = connections.FirstOrDefault(c => c.Service == SocialAlbumProvider.Provider && c.UserId == did);
        var photos = await socialAlbums.GetPhotosAsync(User.Id()!, did, owned, HttpContext.RequestAborted);

        var baseUri = $"{Request.Scheme}://{Request.Host}";
        var feed = new LiveAlbumFeed
        {
            Id = $"{baseUri}/Users({id})/Files/folders('{folderId}')",
            Title = owned != null ? SocialAlbumProvider.TitleFor(owned) : did,
            Updated = DateTime.UtcNow,
            Type = FolderType,
            ResourceId = folderId,
            Category = PhotosCategory,
            SharingLevel = "private",
            ItemCount = photos.Count,
        };

        foreach (var photo in photos)
        {
            var mediaBase = $"{baseUri}/Users({id})/Files/files('{photo.ResourceRef}')";
            var entry = new LiveAlbumItemEntry
            {
                Id = mediaBase,
                ResourceId = photo.ResourceRef,
                Type = "Photo",
                Title = photo.FileName,
                Summary = photo.Summary,
                CommentsEnabled = false,
                Updated = photo.Created,
                Published = photo.Created,
            };

            foreach (var size in ThumbnailSizes)
            {
                entry.Thumbnails.Add(new LiveMediaThumbnail
                {
                    Url = $"{mediaBase}/thumbnail/{size}",
                    MaxWidth = size,
                    Width = photo.Width,
                    Height = photo.Height,
                });
            }

            feed.Entries.Add(entry);
        }

        return Ok(feed);
    }

    [HttpGet]
    [Route("/Users({id})/Files/files('{resourceRef}')/thumbnail/{size:int}")]
    public Task<ActionResult> GetThumbnail(string id, string resourceRef, int size)
        => StreamPhotoAsync(resourceRef, size);

    [HttpGet]
    [Route("/Users({id})/Files/files('{resourceRef}')/media")]
    public Task<ActionResult> GetMedia(string id, string resourceRef)
        => StreamPhotoAsync(resourceRef, 0);

    private async Task<ActionResult> StreamPhotoAsync(string resourceRef, int maxSize)
    {
        var ct = HttpContext.RequestAborted;
        using var http = httpClientFactory.CreateClient();

        if (SocialAlbumProvider.TryParsePhotoRef(resourceRef, out var did, out var cid))
        {
            var social = SocialAlbumProvider.GetMediaLocation(did, cid, maxSize);
            using var socialResponse = await http.FetchAsync(social, HttpContext, ct);

            await socialResponse.PipeAsync(social, HttpContext, ct);
            return new EmptyResult();
        }

        var location = await ResolvePhotoAsync(resourceRef, maxSize, refresh: false, ct);
        if (location == null)
            return NotFound();

        var response = await http.FetchAsync(location.Value, HttpContext, ct);
        try
        {
            // the provider urls are short lived, so a stale one looks exactly like a dead item.
            if (!response.IsSuccessStatusCode)
            {
                var refreshed = await ResolvePhotoAsync(resourceRef, maxSize, refresh: true, ct);
                if (refreshed == null)
                    return NotFound();

                response.Dispose();
                location = refreshed;
                response = await http.FetchAsync(location.Value, HttpContext, ct);
            }

            await response.PipeAsync(location.Value, HttpContext, ct);
            return new EmptyResult();
        }
        finally
        {
            response.Dispose();
        }
    }

    private async Task<ContentLocation?> ResolvePhotoAsync(string resourceRef, int maxSize, bool refresh, CancellationToken ct)
    {
        var reply = await skyDrive.GetPhotoContentAsync(new GetPhotoContentRequest
        {
            UserId = User.Id(),
            ResourceRef = resourceRef,
            MaxSize = maxSize,
            Refresh = refresh,
        }, cancellationToken: ct);

        if (!reply.Exists)
            return null;

        return new ContentLocation(reply.Url, reply.Headers, reply.ContentType,
                                   string.IsNullOrEmpty(reply.Etag) ? null : reply.Etag);
    }

    [HttpPost]
    [Route("/Users({id})/Files/provision")]
    public async Task<ActionResult> Provision(string id)
    {
        await skyDrive.ProvisionUserAsync(new ProvisionUserRequest { UserId = User.Id() });
        return Ok();
    }

    [HttpPost]
    [Route("/Users({id})/Files/{album:regex(^(wmphotos|mobilephotos|twitterphotos)$)}/permissions")]
    public async Task<ActionResult> SetPermissions(string id, string album)
    {
        await skyDrive.SetAlbumPermissionsAsync(new SetAlbumPermissionsRequest
        {
            UserId = User.Id(),
            Category = album,
            SharingLevel = PublicSharedLevel,
        });

        return Ok();
    }

    private LiveLibraryEntry ToEntry(string id, string album, Library library) => new()
    {
        Id = $"{Request.Scheme}://{Request.Host}/Users({id})/Files/{album}",
        ResourceId = library.Id,
        Type = FolderType,
        CanonicalName = CanonicalNameFor(library.Category)!,
        Category = PhotosCategory,
        SharingLevel = library.SharingLevel,
        EmailKeyword = library.EmailKeyword,
        Title = library.Title,
        Summary = library.Summary,
        Updated = DateTime.UtcNow,
    };
}
