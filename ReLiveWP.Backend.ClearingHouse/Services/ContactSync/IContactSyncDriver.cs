namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public sealed record SyncConnection(string UserId, string ConnectionId, string? ServiceUrl = null);

public sealed record ContactSyncBatch(
    IReadOnlyList<RemoteContact> Contacts,
    IReadOnlyList<string> DeletedExternalIds,
    string? DeltaToken,
    bool IsFullSync,
    IReadOnlyList<string>? UnreadableExternalIds = null)
{
    public IReadOnlyList<string> Unreadable => UnreadableExternalIds ?? [];
}

public interface IContactSyncDriver
{
    string ServiceId { get; }

    Task<IReadOnlyList<RemoteContactSource>> ListSourcesAsync(
        SyncConnection connection, CancellationToken ct = default);

    Task<ContactSyncBatch> FetchChangesAsync(
        SyncConnection connection, string sourceId, string? deltaToken, CancellationToken ct = default);
}

public sealed class ContactSyncDriverRegistry(IEnumerable<IContactSyncDriver> drivers)
{
    private readonly Dictionary<string, IContactSyncDriver> _byService =
        drivers.ToDictionary(d => d.ServiceId, StringComparer.OrdinalIgnoreCase);

    public bool TryGet(string serviceId, out IContactSyncDriver driver) =>
        _byService.TryGetValue(serviceId, out driver!);

    public IReadOnlyCollection<string> ServiceIds => _byService.Keys;
}

public class ContactSyncException(string message) : Exception(message);

public class DeltaTokenExpiredException(string message) : ContactSyncException(message);
