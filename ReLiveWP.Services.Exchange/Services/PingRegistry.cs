using System.Collections.Concurrent;
using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Services;

public class PingRegistry
{
    public record CachedPing(int HeartbeatInterval, List<PingFolder> Folders);

    private readonly ConcurrentDictionary<string, CachedPing> _cache = new();

    private static string Key(string userId, string deviceId) => $"{userId}|{deviceId}";

    public CachedPing? Get(string userId, string deviceId) =>
        _cache.TryGetValue(Key(userId, deviceId), out var v) ? v : null;

    public void Store(string userId, string deviceId, int heartbeatInterval, List<PingFolder> folders) =>
        _cache[Key(userId, deviceId)] = new CachedPing(heartbeatInterval, folders);
}
