using System.Collections.Concurrent;

namespace ReLiveWP.Services.MediaProxy.Utilities;

public sealed class SingleFlight<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> inFlight = new();

    public int InFlightCount => inFlight.Count;

    // the work runs detached from any one caller, so one caller hanging up doesn't cancel the rest
    public async Task<TValue> RunOnceAsync(TKey key, Func<Task<TValue>> work, CancellationToken ct)
    {
        Lazy<Task<TValue>>? entry = null;
        entry = new Lazy<Task<TValue>>(async () =>
        {
            try
            {
                return await work();
            }
            finally
            {
                inFlight.TryRemove(new KeyValuePair<TKey, Lazy<Task<TValue>>>(key, entry!));
            }
        });

        var shared = inFlight.GetOrAdd(key, entry);
        return await shared.Value.WaitAsync(ct);
    }
}
