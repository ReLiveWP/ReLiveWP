using System.Xml.Linq;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using ReLiveWP.Identity;
using ReLiveWP.Services.Activity.Models.Atom;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Controllers;

[Controller]
[Authorize]
[Produces("application/atom+xml")]
public class FilesController(SkyDrive.SkyDriveClient skyDrive) : Controller
{
    private const string AlbumRoute = "/Users({id})/Files/{album:regex(^(wmphotos|mobilephotos|twitterphotos)$)}";
    private const string PhotosCategory = "photos";
    private const string FolderType = "Folder";

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
            TotalQuota = 25L * 1024 * 1024 * 1024,
            MaxFileSize = 100L * 1024 * 1024,
            QuotaUsed = 0,
        };

        foreach (var library in reply.Libraries)
            feed.Entries.Add(ToEntry(id, library.Category, library));

        return Ok(feed);
    }

    [HttpGet]
    [Route(AlbumRoute)]
    public async Task<ActionResult> GetAlbum(string id, string album)
    {
        var reply = await skyDrive.GetLibraryAsync(new GetLibraryRequest
        {
            UserId = User.Id(),
            Category = album,
        });

        if (!reply.Exists)
            return NotFound();

        var feed = new LiveLibraryFeed
        {
            Id = $"{Request.Scheme}://{Request.Host}/Users({id})/Files/{album}",
            Updated = DateTime.UtcNow,
        };
        feed.Entries.Add(ToEntry(id, album, reply.Library));
        return Ok(feed);
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

        string? fileName = null;
        string? summary = null;
        byte[]? imageData = null;
        string? imageContentType = null;

        for (var section = await reader.ReadNextSectionAsync(HttpContext.RequestAborted);
             section != null;
             section = await reader.ReadNextSectionAsync(HttpContext.RequestAborted))
        {
            var sectionType = section.ContentType ?? "";
            if (sectionType.Contains("atom+xml", StringComparison.OrdinalIgnoreCase))
            {
                var doc = await XDocument.LoadAsync(section.Body, LoadOptions.None, HttpContext.RequestAborted);
                XNamespace atom = Constants.Atom_Namespace;
                fileName = doc.Root?.Element(atom + "title")?.Value;
                summary = doc.Root?.Element(atom + "summary")?.Value;
            }
            else
            {
                using var ms = new MemoryStream();
                await section.Body.CopyToAsync(ms, HttpContext.RequestAborted);
                imageData = ms.ToArray();
                imageContentType = string.IsNullOrEmpty(sectionType) ? "image/jpeg" : sectionType;
            }
        }

        if (imageData is null || imageData.Length == 0)
            return BadRequest();

        fileName = string.IsNullOrWhiteSpace(fileName) ? $"{Guid.NewGuid():N}.jpg" : fileName;
        imageContentType ??= "image/jpeg";
        
        var auth = Request.Headers.Authorization.ToString();
        var authHeader = string.Concat("Bearer ", auth.AsSpan(auth.IndexOf(' ')));
        var headers = new Metadata { { "Authorization", authHeader } };

        using var call = skyDrive.UploadPhoto(headers, cancellationToken: HttpContext.RequestAborted);
        await call.RequestStream.WriteAsync(new UploadPhotoRequest
        {
            Metadata = new PhotoUploadMetadata
            {
                UserId = userId,
                Category = album,
                FileName = fileName,
                ContentType = imageContentType,
                Summary = summary ?? "",
            }
        });

        const int chunkSize = 64 * 1024;
        for (var offset = 0; offset < imageData.Length; offset += chunkSize)
        {
            var length = Math.Min(chunkSize, imageData.Length - offset);
            await call.RequestStream.WriteAsync(new UploadPhotoRequest
            {
                Chunk = ByteString.CopyFrom(imageData, offset, length)
            });
        }

        await call.RequestStream.CompleteAsync();
        var created = await call.ResponseAsync;

        var entry = new LivePhotoEntry
        {
            Id = $"{Request.Scheme}://{Request.Host}/Users({id})/Files/files('{created.Id}')",
            ResourceId = created.Id,
            Title = fileName,
            Updated = DateTime.UtcNow,
        };

        return Ok(entry);
    }

    [HttpPost]
    [Route("/Users({id})/Files/provision")]
    public async Task<ActionResult> Provision(string id)
    {
        await skyDrive.ProvisionUserAsync(new ProvisionUserRequest { UserId = User.Id() });
        return Ok();
    }

    [HttpPost]
    [Route("/Users({id})/Files/{album}/permissions")]
    public ActionResult SetPermissions(string id, string album)
    {
        // TODO: this
        return Ok();
    }

    private LiveLibraryEntry ToEntry(string id, string album, Library library) => new()
    {
        Id = $"{Request.Scheme}://{Request.Host}/Users({id})/Files/{album}",
        ResourceId = library.Id,
        Type = FolderType,
        Category = PhotosCategory,
        SharingLevel = library.SharingLevel,
        EmailKeyword = library.EmailKeyword,
        Title = library.Title,
        Summary = library.Summary,
        Updated = DateTime.UtcNow,
    };
}
