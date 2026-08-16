using System.Text.Json;
using ReLiveWP.Services.Exchange.Models;
using StackExchange.Redis;

namespace ReLiveWP.Services.Exchange.Services;

public sealed record CachedSyncRequest
{
    public bool Replayable { get; init; }
    public int? Wait { get; init; }
    public int? HeartbeatInterval { get; init; }
    public List<CachedSyncCollection> Collections { get; init; } = [];
}

public sealed record CachedSyncCollection
{
    public string CollectionId { get; init; } = string.Empty;
    public string SyncKey { get; init; } = "0";
    public string? Class { get; init; }
    public bool? GetChanges { get; init; }
    public int? DeletesAsMoves { get; init; }
    public int? WindowSize { get; init; }
    public int? ConversationMode { get; init; }
    public CachedSyncOptions? Options { get; init; }
}

public sealed record CachedSyncOptions
{
    public int? FilterType { get; init; }
    public int? Conflict { get; init; }
    public int? MimeSupport { get; init; }
    public int? MimeTruncation { get; init; }
    public List<CachedBodyPreference> BodyPreference { get; init; } = [];
    public List<CachedBodyPreference> BodyPartPreference { get; init; } = [];
    public List<string> Annotations { get; init; } = [];
}

public sealed record CachedBodyPreference
{
    public int Type { get; init; }
    public int? TruncationSize { get; init; }
    public int? AllOrNone { get; init; }
    public int? Preview { get; init; }
}

public interface ISyncRequestCache
{
    Task<CachedSyncRequest?> GetAsync(string userId, string deviceId, CancellationToken ct = default);
    Task StoreAsync(string userId, string deviceId, CachedSyncRequest request, CancellationToken ct = default);
    Task DisarmAsync(string userId, string deviceId, CancellationToken ct = default);
}

public sealed class RedisSyncRequestCache(
    IConnectionMultiplexer redis,
    ILogger<RedisSyncRequestCache> logger) : ISyncRequestCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);
    private const string SkeletonField = "skeleton";
    private const string ArmedField = "armed";

    private static RedisKey Key(string userId, string deviceId) => $"eas:sync:req:{userId}:{deviceId}";

    // every operation is best effort: losing the cache costs the client one Status 13 and a
    // resend, so a Redis blip must not fail a Sync that otherwise worked
    public async Task<CachedSyncRequest?> GetAsync(string userId, string deviceId, CancellationToken ct = default)
    {
        try
        {
            var values = await redis.GetDatabase().HashGetAsync(Key(userId, deviceId), [SkeletonField, ArmedField]);
            if (values[0].IsNullOrEmpty) return null;

            var skeleton = JsonSerializer.Deserialize<CachedSyncRequest>((byte[])values[0]!);
            return skeleton is null ? null : skeleton with { Replayable = values[1] == "1" };
        }
        catch (Exception e) when (e is JsonException or RedisException)
        {
            logger.LogWarning(e, "discarding cached Sync request for {Device}", deviceId);
            return null;
        }
    }

    public Task StoreAsync(string userId, string deviceId, CachedSyncRequest request, CancellationToken ct = default) =>
        WriteAsync(deviceId, (db, key) => db.HashSetAsync(key,
        [
            new HashEntry(SkeletonField, JsonSerializer.SerializeToUtf8Bytes(request)),
            new HashEntry(ArmedField, request.Replayable ? "1" : "0"),
        ]), Key(userId, deviceId));

    public Task DisarmAsync(string userId, string deviceId, CancellationToken ct = default) =>
        WriteAsync(deviceId, (db, key) => db.HashSetAsync(key, ArmedField, "0"), Key(userId, deviceId));

    private async Task WriteAsync(string deviceId, Func<IDatabase, RedisKey, Task> write, RedisKey key)
    {
        try
        {
            var db = redis.GetDatabase();
            await write(db, key);
            await db.KeyExpireAsync(key, Ttl);
        }
        catch (RedisException e)
        {
            logger.LogWarning(e, "could not update the cached Sync request for {Device}", deviceId);
        }
    }
}

public static class SyncRequestCacheMapping
{
    public static CachedSyncRequest ToCached(Sync request, IEnumerable<SyncCollection> collections, bool replayable) => new()
    {
        Replayable = replayable,
        Wait = request.Wait,
        HeartbeatInterval = request.HeartbeatInterval,
        Collections = [.. collections.Select(ToCached)],
    };

    public static CachedSyncCollection ToCached(SyncCollection c) => new()
    {
        CollectionId = c.CollectionId,
        SyncKey = c.SyncKey,
        Class = c.Class,
        GetChanges = c.GetChanges,
        DeletesAsMoves = c.DeletesAsMoves,
        WindowSize = c.WindowSize,
        ConversationMode = c.ConversationMode,
        Options = ToCached(c.Options),
    };

    public static SyncCollection ToRequest(CachedSyncCollection c) => new()
    {
        CollectionId = c.CollectionId,
        SyncKey = c.SyncKey,
        Class = c.Class,
        GetChanges = c.GetChanges,
        DeletesAsMoves = c.DeletesAsMoves,
        WindowSize = c.WindowSize,
        ConversationMode = c.ConversationMode,
        Options = ToRequest(c.Options),
    };

    private static CachedSyncOptions? ToCached(SyncOptions? o) => o is null ? null : new CachedSyncOptions
    {
        FilterType = o.FilterTypeInt,
        Conflict = o.ConflictInt,
        MimeSupport = o.MIMESupportInt,
        MimeTruncation = o.MIMETruncation,
        BodyPreference = [.. o.BodyPreference.Select(ToCached)],
        BodyPartPreference = [.. o.BodyPartPreference.Select(ToCached)],
        Annotations = [.. o.Annotations?.Items.Select(a => a.Name) ?? []],
    };

    private static SyncOptions? ToRequest(CachedSyncOptions? o) => o is null ? null : new SyncOptions
    {
        FilterTypeInt = o.FilterType,
        ConflictInt = o.Conflict,
        MIMESupportInt = o.MimeSupport,
        MIMETruncation = o.MimeTruncation,
        BodyPreference = [.. o.BodyPreference.Select(ToRequest)],
        BodyPartPreference = [.. o.BodyPartPreference.Select(ToRequest)],
        Annotations = o.Annotations.Count == 0
            ? null
            : new Annotations { Items = [.. o.Annotations.Select(n => new Annotation { Name = n })] },
    };

    private static CachedBodyPreference ToCached(BodyPreference p) => new()
    {
        Type = p.TypeInt,
        TruncationSize = p.TruncationSize,
        AllOrNone = p.AllOrNone,
        Preview = p.Preview,
    };

    private static BodyPreference ToRequest(CachedBodyPreference p) => new()
    {
        TypeInt = p.Type,
        TruncationSize = p.TruncationSize,
        AllOrNone = p.AllOrNone,
        Preview = p.Preview,
    };
}
