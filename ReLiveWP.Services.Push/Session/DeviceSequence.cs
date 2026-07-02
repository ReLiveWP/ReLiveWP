using StackExchange.Redis;

namespace ReLiveWP.Services.Push.Session;

public class DeviceSequence(IConnectionMultiplexer redis, string deviceId)
{
    private const int BlockSize = 64;
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(7);

    private readonly IDatabase db = redis.GetDatabase();
    private readonly string key = $"push:seq:{deviceId}";
    private readonly SemaphoreSlim gate = new(1, 1);

    private long next; // next value to hand out
    private long max;  // exclusive top of the currently-reserved block

    public async ValueTask<uint> NextAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (next >= max)
            {
                var top = await db.StringIncrementAsync(key, BlockSize);
                await db.KeyExpireAsync(key, Ttl);
                next = top - BlockSize + 1;
                max = top + 1;
            }
            return (uint)next++;
        }
        finally
        {
            gate.Release();
        }
    }
}
