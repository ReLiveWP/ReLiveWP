using System.Text.Json;
using ReLiveWP.Backend.Skybox.Commands;
using StackExchange.Redis;

namespace ReLiveWP.Backend.Skybox.Services;

public enum CommandState { Pending, Active, Final }

public record CommandRecord(
    uint RequestId,
    DeviceCommandAction Action,
    Guid UserId,
    string DeviceId,
    DateTimeOffset CreatedAt,
    CommandState State);

public class CommandRegistry(IConnectionMultiplexer redis)
{
    private static readonly TimeSpan RecordTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan SeqTtl = TimeSpan.FromDays(30);

    private readonly IDatabase db = redis.GetDatabase();

    private static string SeqKey(string deviceId) => $"sky:cmd-seq:{deviceId}";
    private static string RecordKey(string deviceId, uint requestId) => $"sky:cmd:{deviceId}:{requestId}";

    public async Task<CommandRecord> CreateAsync(string deviceId, Guid userId, DeviceCommandAction action)
    {
        var requestId = (uint)await db.StringIncrementAsync(SeqKey(deviceId));
        await db.KeyExpireAsync(SeqKey(deviceId), SeqTtl);

        var record = new CommandRecord(requestId, action, userId, deviceId, DateTimeOffset.UtcNow, CommandState.Pending);
        await db.StringSetAsync(RecordKey(deviceId, requestId), JsonSerializer.Serialize(record), RecordTtl);
        return record;
    }

    public async Task<CommandRecord?> GetAsync(string deviceId, uint requestId)
    {
        var value = await db.StringGetAsync(RecordKey(deviceId, requestId));
        return value.IsNull ? null : JsonSerializer.Deserialize<CommandRecord>((string)value!);
    }

    public Task SetStateAsync(CommandRecord record, CommandState state) =>
        db.StringSetAsync(RecordKey(record.DeviceId, record.RequestId),
            JsonSerializer.Serialize(record with { State = state }), RecordTtl);
}
