using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using ReLiveWP.ServiceDefaults.Media;

namespace ReLiveWP.Services.MediaProxy.Models;

public sealed record RemoteMediaResult(int StatusCode, byte[]? Jpeg, string? ETag)
{
    private const int ETagBytes = 16;

    public static RemoteMediaResult UpstreamFailed { get; } = new(StatusCodes.Status502BadGateway, null, null);

    public static RemoteMediaResult FromJpeg(byte[] jpeg)
    {
        var hash = SHA256.HashData(jpeg)[..ETagBytes];
        var etag = $"\"{WebEncoders.Base64UrlEncode(hash)}\"";
        return new RemoteMediaResult(StatusCodes.Status200OK, jpeg, etag);
    }

    public static RemoteMediaResult FromRejection(string code)
    {
        var status = code == MediaPipelineRejection.Unreadable
            ? StatusCodes.Status415UnsupportedMediaType
            : StatusCodes.Status422UnprocessableEntity;

        return new RemoteMediaResult(status, null, null);
    }
}
