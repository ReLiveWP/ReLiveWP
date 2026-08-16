using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Services;

public record SocialAlbum(string ResourceId, string Title);
public record SocialPhoto(string ResourceRef, string FileName, string? Summary, DateTime Created, int Width, int Height);
public record SocialAlbumContents(IReadOnlyList<SocialPhoto> Photos, string? Handle);

public abstract class SocialAlbumProviderBase
{
    public static readonly TimeSpan FeedLifetime = TimeSpan.FromMinutes(2);

    public abstract string Provider { get; }

    public static string CacheKey(string provider, string externalId, Connection? connection)
        => connection == null
            ? $"social:{provider}:{externalId}"
            : $"social:owned:{connection.Id}:{provider}:{externalId}";

    public abstract bool IsValidExternalId(string externalId);
    public abstract bool IsValidMediaId(string mediaId);

    public abstract Task<IReadOnlyList<SocialAlbum>> GetAlbumsAsync(
        string userId, IEnumerable<Connection> connections, CancellationToken ct = default);

    public abstract Task<SocialAlbumContents> GetAlbumAsync(
        string userId, string externalId, Connection? connection, CancellationToken ct = default);

    public abstract Task<string?> GetHandleAsync(
        string userId, string externalId, Connection? connection, CancellationToken ct = default);

    public abstract ContentLocation GetMediaLocation(string externalId, string mediaId, int maxSize);

    public virtual string FileNameFor(string mediaId) => $"{mediaId}.jpg";

    public virtual string TitleFor(string? handle) =>
        string.IsNullOrWhiteSpace(handle) ? "photos" : $"@{handle.TrimStart('@')}'s photos";
}
