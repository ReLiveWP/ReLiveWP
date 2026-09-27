using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Activity.Models.Atom;
using ReLiveWP.Services.Activity.Providers;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Activity.Utilities;

namespace ReLiveWP.Services.Activity.Controllers;

[Controller]
[Authorize]
[Produces("application/atom+xml")]
public class FilesController(FileViewerService viewer,
                             PhotoLibraryService libraries,
                             SocialAlbumsService socialAlbums,
                             SocialAlbumService social,
                             ConnectionLookupService connections,
                             PhotoUploadService uploads,
                             PhotoStreamService streams) : Controller
{
    private string UserId => User.Id()!;
    private CancellationToken Aborted => HttpContext.RequestAborted;
    private FilesUrls Urls(string id) => FilesUrls.For(Request, id);

    [HttpGet]
    [Route("/Users({id})/Files")]
    public async Task<ActionResult> GetFiles(string id)
    {
        var urls = Urls(id);
        var feed = PhotoFeedRenderer.CreateListing(urls);

        // a contact only ever has the albums they chose to share, never anything of ours
        if (await viewer.SubjectCidAsync(id, User, Aborted) is { } subjectCid)
        {
            await AddSocialAlbumsAsync(feed, urls, await social.SharedAlbumsAsync(subjectCid, UserId, Aborted));
            return Ok(feed);
        }

        var owned = await libraries.ListLibrariesAsync(UserId, Aborted);
        if (await connections.HasPhotoSyncAsync(Aborted))
        {
            foreach (var library in owned)
                feed.Entries.Add(PhotoFeedRenderer.CreateLibraryEntry(urls, library));
        }

        await AddSocialAlbumsAsync(feed, urls, await social.OwnedAlbumsAsync(UserId, Aborted));
        return Ok(feed);
    }

    [HttpGet]
    [Route("/Users({id})/PhotosOf")]
    public ActionResult PhotosOf(string id)
    {
        // TODO: photo tagging
        return Ok(new LiveTaggedPhotosFeed());
    }

    [HttpGet]
    [Route(PhotoAlbums.AlbumRoute)]
    public async Task<ActionResult> GetAlbum(string id, string album)
    {
        if (await viewer.SubjectCidAsync(id, User, Aborted) != null)
            return NotFound();

        var listing = await libraries.ListByCategoryAsync(UserId, album, Aborted);
        if (listing == null)
            return NotFound();

        var urls = Urls(id);
        return Ok(PhotoFeedRenderer.CreateAlbumFeed(urls, urls.ForAlbum(album), listing.Library, listing.Photos));
    }

    [HttpPut]
    [Route(PhotoAlbums.AlbumRoute)]
    public async Task<ActionResult> PutAlbum(string id, string album, [FromBody] LiveLibraryEntry? entry)
    {
        if (entry is null || !ModelState.IsValid)
            return BadRequest();

        if (await viewer.SubjectCidAsync(id, User, Aborted) != null)
            return NotFound();

        var library = await libraries.CreateOrUpdateAsync(UserId, album, new LibraryUpdate(
            entry.Type ?? "Library",
            entry.SharingLevel ?? PhotoAlbums.PrivateSharing,
            entry.Title?.Value ?? "",
            entry.Summary?.Value ?? "",
            entry.EmailKeyword ?? ""), Aborted);

        return Ok(PhotoFeedRenderer.CreateLibraryEntry(Urls(id), library));
    }

    [HttpPost]
    [Route(PhotoAlbums.AlbumRoute)]
    public async Task<ActionResult> UploadPhoto(string id, string album)
    {
        if (await viewer.SubjectCidAsync(id, User, Aborted) != null)
            return NotFound();

        if (!PhotoUploadReader.IsMultipartRelated(Request.ContentType, out var boundary))
            return StatusCode(StatusCodes.Status415UnsupportedMediaType);

        if (string.IsNullOrEmpty(boundary))
            return BadRequest();

        try
        {
            await using var upload = await PhotoUploadReader.ReadAsync(Request.Body, boundary, UserId, album, Aborted);
            if (upload == null)
                return BadRequest();

            var created = await uploads.UploadAsync(upload.Spool, upload.Metadata, Aborted);
            if (created == null)
                return BadRequest();

            return Ok(PhotoFeedRenderer.CreateUploadedEntry(Urls(id), created));
        }
        catch (IOException)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }
    }

    [HttpGet]
    [Route("/Users({id})/Files/folders('{folderId}')")]
    public async Task<ActionResult> GetFolder(string id, string folderId)
    {
        if (socialAlbums.TryResolveAlbum(folderId, out var provider, out var externalId))
            return await GetSocialFolderAsync(id, folderId, provider, externalId);

        if (await viewer.SubjectCidAsync(id, User, Aborted) != null)
            return NotFound();

        var listing = await libraries.ListByFolderAsync(UserId, folderId, Aborted);
        if (listing == null)
            return NotFound();

        var urls = Urls(id);
        return Ok(PhotoFeedRenderer.CreateAlbumFeed(urls, urls.ForFolder(folderId), listing.Library, listing.Photos));
    }

    [HttpGet]
    [Route("/Users({id})/Files/files('{resourceRef}')")]
    public async Task<ActionResult> GetPhoto(string id, string resourceRef)
    {
        string? title = null;

        if (socialAlbums.TryResolvePhoto(resourceRef, out var provider, out var externalId, out var mediaId))
        {
            var subjectCid = await viewer.SubjectCidAsync(id, User, Aborted);
            if (!await social.CanServeAsync(provider, externalId, subjectCid, UserId, Aborted))
                return NotFound();

            title = provider.FileNameFor(mediaId);
        }

        return Ok(PhotoFeedRenderer.CreatePhotoFeed(Urls(id), resourceRef, title));
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = MediaTicketAuthHandler.OrLiveID)]
    [Route("/Users({id})/Files/files('{resourceRef}')/thumbnail/{size:int}")]
    public Task<ActionResult> GetThumbnail(string id, string resourceRef, int size)
    {
        return StreamPhotoAsync(id, resourceRef, size);
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = MediaTicketAuthHandler.OrLiveID)]
    [Route("/Users({id})/Files/files('{resourceRef}')/media")]
    public Task<ActionResult> GetMedia(string id, string resourceRef)
    {
        return StreamPhotoAsync(id, resourceRef, 0);
    }

    [HttpPost]
    [Route("/Users({id})/Files/provision")]
    public async Task<ActionResult> Provision(string id)
    {
        if (await viewer.SubjectCidAsync(id, User, Aborted) != null)
            return NotFound();

        await libraries.ProvisionAsync(UserId, Aborted);
        return Ok();
    }

    [HttpPost]
    [Route(PhotoAlbums.PermissionsRoute)]
    public async Task<ActionResult> SetPermissions(string id, string album)
    {
        if (await viewer.SubjectCidAsync(id, User, Aborted) != null)
            return NotFound();

        await libraries.ShareAsync(UserId, album, Aborted);
        return Ok();
    }

    private async Task<ActionResult> GetSocialFolderAsync(
        string id, string folderId, SocialAlbumProviderBase provider, string externalId)
    {
        var subjectCid = await viewer.SubjectCidAsync(id, User, Aborted);
        if (!await social.CanServeAsync(provider, externalId, subjectCid, UserId, Aborted))
            return NotFound();

        var folder = await social.FolderAsync(provider, externalId, UserId, Aborted);
        await libraries.RememberCoverAsync(UserId, folderId, folder.Photos.FirstOrDefault()?.ResourceRef, Aborted);

        return Ok(PhotoFeedRenderer.CreateSocialAlbumFeed(Urls(id), folderId, folder.Title, folder.Photos));
    }

    private async Task AddSocialAlbumsAsync(LiveLibraryFeed feed, FilesUrls urls, IReadOnlyList<SocialAlbum> albums)
    {
        var covers = await libraries.CoversAsync(UserId, [.. albums.Select(a => a.ResourceId)], Aborted);

        foreach (var album in albums)
            feed.Entries.Add(PhotoFeedRenderer.CreateSocialAlbumEntry(urls, album, covers.GetValueOrDefault(album.ResourceId)));
    }

    private async Task<ActionResult> StreamPhotoAsync(string id, string resourceRef, int maxSize)
    {
        return await streams.WriteAsync(HttpContext, id, resourceRef, maxSize, Aborted)
            ? new EmptyResult()
            : NotFound();
    }
}
