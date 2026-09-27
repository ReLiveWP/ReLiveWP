namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror;

public interface IMirrorDriver
{
    string ServiceId { get; }

    MirrorKind Kind { get; }

    Task<IReadOnlyList<RemoteSource>> ListSourcesAsync(
        SyncConnection connection, CancellationToken ct = default);

    Task<MirrorBatch> FetchChangesAsync(
        SyncConnection connection, string sourceId, string? deltaToken, CancellationToken ct = default);
}

public sealed class MirrorDriverRegistry(IEnumerable<IMirrorDriver> drivers)
{
    private readonly Dictionary<(MirrorKind, string), IMirrorDriver> _byKindAndService =
        drivers.ToDictionary(d => (d.Kind, d.ServiceId.ToLowerInvariant()));

    public bool TryGet(MirrorKind kind, string serviceId, out IMirrorDriver driver) =>
        _byKindAndService.TryGetValue((kind, serviceId.ToLowerInvariant()), out driver!);

    public IReadOnlyCollection<string> ServiceIdsFor(MirrorKind kind) =>
        [.. _byKindAndService.Keys.Where(k => k.Item1 == kind).Select(k => k.Item2)];
}

public class MirrorException(string message) : Exception(message);

public class DeltaTokenExpiredException(string message) : MirrorException(message);
