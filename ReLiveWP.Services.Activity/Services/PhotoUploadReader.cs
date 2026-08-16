using System.Xml.Linq;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using ReLiveWP.Services.Activity.Models.Atom;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Services;

public sealed record PhotoUpload(FileBufferingReadStream Spool, PhotoUploadMetadata Metadata) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Spool.DisposeAsync();
}

public static class PhotoUploadReader
{
    public const long MaxFileSize = 100L * 1024 * 1024;

    private const int SpoolMemoryThreshold = 1024 * 1024;
    private const string MultipartRelated = "multipart/related";

    public static bool IsMultipartRelated(string? contentType, out string? boundary)
    {
        boundary = null;

        if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaType) ||
            !mediaType.MediaType.Equals(MultipartRelated, StringComparison.OrdinalIgnoreCase))
            return false;

        boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
        return true;
    }

    public static async Task<PhotoUpload?> ReadAsync(Stream body, string boundary, string userId, string album,
                                                     CancellationToken ct = default)
    {
        var reader = new MultipartReader(boundary, body);

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
        }
        catch
        {
            if (spool != null)
                await spool.DisposeAsync();

            throw;
        }

        if (spool is null || spool.Length == 0)
        {
            if (spool != null)
                await spool.DisposeAsync();

            return null;
        }

        var mediaType = MediaKinds.IsVideo(liveType) ? MediaKinds.Video : MediaKinds.Photo;

        return new PhotoUpload(spool, new PhotoUploadMetadata
        {
            UserId = userId,
            Category = album,
            FileName = string.IsNullOrWhiteSpace(fileName) ? GenerateFileName(mediaType) : fileName,
            ContentType = imageContentType ?? NormaliseContentType("", mediaType),
            Summary = summary ?? "",
            MediaType = mediaType,
            ResolveNameConflict = resolveNameConflict,
            SuppressNotification = suppressNotification,
        });
    }

    private static string GenerateFileName(string mediaType)
        => $"{Guid.NewGuid():N}{(MediaKinds.IsVideo(mediaType) ? ".mp4" : ".jpg")}";

    private static bool ParseLiveBool(string? value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";

    private static string NormaliseContentType(string sectionType, string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(sectionType) || !sectionType.Contains('/'))
            return MediaKinds.IsVideo(mediaType) ? "video/mp4" : "image/jpeg";

        return sectionType;
    }
}
