namespace ReLiveWP.Backend.SkyDrive.Services;

public record PhotoUploadInfo(string FileName, string ContentType, string? Description, long Length);
public record ProviderUploadTarget(string Method, string Url, IReadOnlyDictionary<string, string> Headers, long FragmentSize);
public record ProviderUploadResult(string ItemId, string? Url);

public record ProviderPhoto(
    string ItemId,
    string FileName,
    string ContentType,
    string? Description,
    DateTimeOffset Created,
    int Width,
    int Height,
    bool IsVideo);

public interface IPhotoSyncProxyClient
{
    string ServiceId { get; }

    Task<string> EnsureAlbumAsync(string userId, string connectionId, string title, CancellationToken ct = default);

    Task<IReadOnlyList<ProviderPhoto>> ListAsync(string userId, string connectionId, string albumId, CancellationToken ct = default);

    Task<ProviderContentLocation?> ResolveContentAsync(string userId, string connectionId, string itemId, int maxSize, bool refresh, CancellationToken ct = default);

    Task<ProviderUploadTarget> BeginUploadAsync(string userId, string connectionId, string albumId, PhotoUploadInfo photo, CancellationToken ct = default);

    Task<ProviderUploadResult> CompleteUploadAsync(string userId, string connectionId, string albumId, PhotoUploadInfo photo, string responseBody, CancellationToken ct = default);
}
