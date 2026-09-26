using Microsoft.Net.Http.Headers;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.MediaProxy.Services;

namespace ReLiveWP.Services.MediaProxy.Endpoints;

public static class PublicEndpoints
{
    public const string ServedCacheControl = "public, max-age=86400, s-maxage=604800";
    public const string NotFoundCacheControl = "public, max-age=60, s-maxage=60";
    public const string UpstreamFailureCacheControl = "public, max-age=30, s-maxage=60";
    public const string RejectedCacheControl = "public, max-age=3600, s-maxage=3600";

    public static void MapPublicEndpoints(this WebApplication app)
    {
        app.MapMethods($"/{{version}}/{{size}}/{{signature}}/{{source}}{MediaProxyUrlSigner.FileExtension}",
                       [HttpMethods.Get, HttpMethods.Head], GetMediaAsync);
    }

    internal static async Task<IResult> GetMediaAsync(
        string version,
        string size,
        string signature,
        string source,
        HttpContext context,
        MediaProxyUrlSigner signer,
        RemoteMediaService media,
        CancellationToken ct)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";

        if (!signer.TryVerifyPath(version, size, signature, source, out var sourceUri, out var mediaSize))
        {
            headers.CacheControl = NotFoundCacheControl;
            return TypedResults.NotFound();
        }

        var result = await media.RenderMediaAsync(sourceUri, mediaSize, ct);
        if (result.Jpeg == null)
        {
            headers.CacheControl = result.StatusCode == StatusCodes.Status502BadGateway
                ? UpstreamFailureCacheControl
                : RejectedCacheControl;

            return TypedResults.StatusCode(result.StatusCode);
        }

        headers.CacheControl = ServedCacheControl;
        var etag = EntityTagHeaderValue.Parse(result.ETag);
        return TypedResults.File(result.Jpeg, MediaProfiles.OutputContentType, entityTag: etag);
    }
}
