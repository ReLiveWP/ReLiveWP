using StackExchange.Redis;

namespace ReLiveWP.Backend.SkyDrive.Services;

public class WebDavUploadStore(IConnectionMultiplexer redis)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    private readonly IDatabase db = redis.GetDatabase();

    private static string Key(string connectionId, string albumId, string fileName)
        => $"webdav:upload:{connectionId}:{albumId}:{fileName}";

    public async Task StashAsync(string connectionId, string albumId, string fileName, string path)
    {
        try
        {
            await db.StringSetAsync(Key(connectionId, albumId, fileName), path, Ttl);
        }
        catch (RedisException ex)
        {
            throw RedisFaults.Unavailable(ex);
        }
    }

    public async Task<string?> TakeAsync(string connectionId, string albumId, string fileName)
    {
        RedisValue value;
        try
        {
            value = await db.StringGetDeleteAsync(Key(connectionId, albumId, fileName));
        }
        catch (RedisException ex)
        {
            throw RedisFaults.Unavailable(ex);
        }

        return value.IsNull ? null : (string)value!;
    }
}
