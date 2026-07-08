using System.Collections.Concurrent;

namespace ReLiveWP.Services.Exchange.Services;
public class OrphanFolderTracker
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _byDevice =
        new(StringComparer.Ordinal);

    public void Record(string deviceId, string collectionId) =>
        _byDevice.GetOrAdd(deviceId, _ => new(StringComparer.Ordinal)).TryAdd(collectionId, 0);
        
    public IReadOnlyList<string> Drain(string deviceId) =>
        _byDevice.TryRemove(deviceId, out var set) ? [.. set.Keys] : [];
}
