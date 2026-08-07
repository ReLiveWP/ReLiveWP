namespace ReLiveWP.Backend.SkyDrive.Services;

public record ProviderLibrary(
    string Path,
    string DisplayName,
    string ResourceId,
    bool ReadOnly,
    DateTimeOffset Modified);

public record ProviderEntry(
    string Name,
    string Path,
    bool IsFolder,
    long Size,
    DateTimeOffset Created,
    DateTimeOffset Modified,
    string ContentType,
    string? ETag,
    bool ReadOnly,
    string ResourceId,
    string? ProgId);

public record ProviderChangeSet(
    IReadOnlyList<ProviderEntry> Changed,
    IReadOnlyList<string> DeletedPaths,
    string? Cursor);

public record ProviderContentLocation(
    string Url,
    IReadOnlyDictionary<string, string> Headers,
    string ContentType,
    long Size,
    string? ETag);

public interface IFileSyncProxyClient
{
    string ServiceId { get; }

    bool SupportsDelta { get; }

    Task<IReadOnlyList<ProviderLibrary>> ListLibrariesAsync(string userId, string connectionId, CancellationToken ct = default);

    Task<ProviderEntry?> GetItemAsync(string userId, string connectionId, string path, CancellationToken ct = default);

    Task<IReadOnlyList<ProviderEntry>> ListChildrenAsync(string userId, string connectionId, string path, bool recursive, CancellationToken ct = default);

    Task<ProviderChangeSet> GetChangesAsync(string userId, string connectionId, string path, string? cursor, CancellationToken ct = default);

    Task<ProviderContentLocation?> GetContentLocationAsync(string userId, string connectionId, string path, CancellationToken ct = default);
}
