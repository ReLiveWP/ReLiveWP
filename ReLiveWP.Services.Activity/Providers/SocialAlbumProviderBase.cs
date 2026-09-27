using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Providers;

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

    // album keys end up in files route paths, so a provider whose identities can't go there names them differently
    public virtual Task<string?> GetAlbumKeyAsync(string identityId, CancellationToken ct = default)
        => Task.FromResult<string?>(identityId);

    public virtual Task<string?> FindIdentityAsync(string albumKey, CancellationToken ct = default)
        => Task.FromResult<string?>(albumKey);

    public abstract Task<IReadOnlyList<SocialAlbum>> GetAlbumsAsync(
        string userId, IEnumerable<Connection> connections, CancellationToken ct = default);

    public abstract Task<SocialAlbumContents> GetAlbumAsync(
        string userId, string externalId, Connection? connection, CancellationToken ct = default);

    public abstract Task<string?> GetHandleAsync(
        string userId, string externalId, Connection? connection, CancellationToken ct = default);

    // the size only picks which of the provider's own renditions to start from, the proxy does the resize
    public abstract Task<Uri?> ResolveMediaSourceAsync(string albumKey, string mediaId, MediaSize size, CancellationToken ct = default);

    public virtual string FileNameFor(string mediaId) => $"{mediaId}.jpg";

    public virtual string TitleFor(string? handle) =>
        string.IsNullOrWhiteSpace(handle) ? "photos" : $"@{handle.TrimStart('@')}'s photos";
}
