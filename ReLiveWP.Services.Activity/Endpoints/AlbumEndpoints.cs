using System.Globalization;
using System.Security.Claims;
using ReLiveWP.Identity;
using ReLiveWP.Services.Activity.Models.Web;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Activity.Utilities;

namespace ReLiveWP.Services.Activity.Endpoints;

public static class AlbumEndpoints
{
    public static void MapAlbumEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/social").RequireAuthorization();

        group.MapGet("/albums", GetOwnAlbumsAsync);
        group.MapGet("/albums/{resourceId}", GetOwnAlbumAsync);
        group.MapGet("/contacts/{cid}/albums", GetContactAlbumsAsync);
        group.MapGet("/contacts/{cid}/albums/{resourceId}", GetContactAlbumAsync);
    }

    private static async Task<IResult> GetOwnAlbumsAsync(
        HttpContext context,
        FileViewerService viewer,
        WebAlbumsService albums,
        CancellationToken ct)
    {
        var urls = await GetOwnedFilesAsync(context, viewer, ct);
        var listed = await albums.ListOwnAlbumsAsync(urls, context.User.Id()!, ct);

        return Results.Ok(new SocialAlbumsResponse(listed));
    }

    private static async Task<IResult> GetOwnAlbumAsync(
        string resourceId,
        HttpContext context,
        FileViewerService viewer,
        WebAlbumsService albums,
        CancellationToken ct)
    {
        var urls = await GetOwnedFilesAsync(context, viewer, ct);
        var album = await albums.OpenOwnAlbumAsync(urls, context.User.Id()!, resourceId, ct);

        return album == null
            ? Results.NotFound(new SocialErrorResponse("album_not_found"))
            : Results.Ok(album);
    }

    private static async Task<IResult> GetContactAlbumsAsync(
        string cid,
        ClaimsPrincipal user,
        WebAlbumsService albums,
        CancellationToken ct)
    {
        if (!WebCids.TryParseCid(cid, out var contactCid))
            return Results.BadRequest(new SocialErrorResponse("invalid_cid"));

        var shared = await albums.ListSharedAlbumsAsync(contactCid, user.Id()!, ct);
        return Results.Ok(new SocialContactAlbumsResponse(WebCids.FormatCid(contactCid), shared.Count > 0, shared));
    }

    private static async Task<IResult> GetContactAlbumAsync(
        string cid,
        string resourceId,
        ClaimsPrincipal user,
        WebAlbumsService albums,
        CancellationToken ct)
    {
        if (!WebCids.TryParseCid(cid, out var contactCid))
            return Results.BadRequest(new SocialErrorResponse("invalid_cid"));

        var album = await albums.OpenSharedAlbumAsync(contactCid, user.Id()!, resourceId, ct);
        return album == null
            ? Results.NotFound(new SocialErrorResponse("album_not_found"))
            : Results.Ok(album);
    }

    private static async Task<FilesUrls> GetOwnedFilesAsync(HttpContext context, FileViewerService viewer, CancellationToken ct)
    {
        var ownerCid = await viewer.OwnerCidAsync(context.User, ct) ?? 0;
        return FilesUrls.For(context.Request, ownerCid.ToString(CultureInfo.InvariantCulture));
    }
}
