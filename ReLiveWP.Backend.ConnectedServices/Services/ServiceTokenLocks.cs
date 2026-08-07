using RedLockNet;
using RedLockNet.SERedis;

namespace ReLiveWP.Backend.ConnectedServices.Services;

public class ServiceTokenLocks(RedLockFactory lockFactory)
{
    private static readonly TimeSpan Expiry = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Retry = TimeSpan.FromMilliseconds(200);

    public Task<IRedLock> AcquireAsync(Guid connectionId, CancellationToken ct = default) =>
        lockFactory.CreateLockAsync($"cs:token-refresh:{connectionId}", Expiry, Wait, Retry, ct);
}
