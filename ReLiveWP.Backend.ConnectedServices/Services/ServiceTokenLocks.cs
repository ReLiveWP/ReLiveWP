using System.Collections.Concurrent;

namespace ReLiveWP.Backend.ConnectedServices.Services;

// In-process semaphores for token refresh serialization.
// Safe for single-instance deployments. For multi-instance: replace with distributed locks (e.g. Redlock)
// and rely on the ConcurrencyCheck on LiveConnectedService.RowVersion as the backstop.
public class ServiceTokenLocks
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public SemaphoreSlim GetOrCreateLock(Guid connectionId) =>
        _locks.GetOrAdd(connectionId, _ => new SemaphoreSlim(1, 1));
}
