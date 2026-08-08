using StackExchange.Redis;

namespace ReLiveWP.Backend.SkyDrive.Services;

public class WebDavUploadStore(IConnectionMultiplexer redis)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    private readonly IDatabase db = redis.GetDatabase();

    private static string Key(string connectionId, string albumId, string fileName)
        => $"webdav:upload:{connectionId}:{albumId}:{fileName}";

    public Task StashAsync(string connectionId, string albumId, string fileName, string path)
        => db.StringSetAsync(Key(connectionId, albumId, fileName), path, Ttl);

    public async Task<string?> TakeAsync(string connectionId, string albumId, string fileName)
    {
        var value = await db.StringGetDeleteAsync(Key(connectionId, albumId, fileName));
        return value.IsNull ? null : (string)value!;
    }
}
