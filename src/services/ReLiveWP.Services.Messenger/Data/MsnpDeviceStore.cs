using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ReLiveWP.Services.Messenger.Data;

public class MsnpDeviceStore(IConnectionMultiplexer redis, IOptions<MessengerOptions> options) : IMsnpDeviceStore
{
    private record DeviceEntry(string NotificationUri, long LastSignInUnixMs);

    private readonly IDatabase db = redis.GetDatabase();

    private static RedisKey DevicesKey(string userId) => $"msnp:gateway:devices:{userId}";
    private static RedisKey DoorbellKey(string userId, string instanceId) =>
        $"msnp:gateway:device-doorbell:{userId}:{instanceId.ToLowerInvariant()}";

    private TimeSpan Retention => options.Value.DeviceRetention;

    public async Task RememberDeviceAsync(string userId, MsnpDevice device)
    {
        var entry = new DeviceEntry(device.NotificationUri, device.LastSignIn.ToUnixTimeMilliseconds());

        var transaction = db.CreateTransaction();
        _ = transaction.HashSetAsync(DevicesKey(userId), device.InstanceId.ToLowerInvariant(), JsonSerializer.Serialize(entry));
        _ = transaction.KeyExpireAsync(DevicesKey(userId), Retention);
        await transaction.ExecuteAsync();
    }

    public Task ForgetDeviceAsync(string userId, string instanceId) =>
        db.HashDeleteAsync(DevicesKey(userId), instanceId.ToLowerInvariant());

    public async Task<IReadOnlyList<MsnpDevice>> ListDevicesAsync(string userId, DateTimeOffset now)
    {
        var devices = new List<MsnpDevice>();
        foreach (var field in await db.HashGetAllAsync(DevicesKey(userId)))
        {
            var entry = JsonSerializer.Deserialize<DeviceEntry>((string)field.Value!);
            var lastSignIn = entry is null ? DateTimeOffset.MinValue : DateTimeOffset.FromUnixTimeMilliseconds(entry.LastSignInUnixMs);
            if (entry is null || lastSignIn + Retention < now)
            {
                await db.HashDeleteAsync(DevicesKey(userId), field.Name);
                continue;
            }

            devices.Add(new MsnpDevice(field.Name!, entry.NotificationUri, lastSignIn));
        }

        return devices;
    }

    public Task<bool> TryClaimDoorbellAsync(string userId, string instanceId, TimeSpan window) =>
        db.StringSetAsync(DoorbellKey(userId, instanceId), 1, window, When.NotExists);
}
