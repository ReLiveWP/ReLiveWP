using System.Collections.Concurrent;

namespace ReLiveWP.Backend.Identity.Services;

public class ServiceTokenLocks
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();
    public SemaphoreSlim GetOrCreateLock(Guid connectionId)
    {
        return _locks.GetOrAdd(connectionId, _ => new SemaphoreSlim(1, 1));
    }
}
