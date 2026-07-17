using System.Text.Json;
using StackExchange.Redis;

namespace ReLiveWP.ServiceDefaults.Events;

// broadcast when a non-device account is created, so backends can spin up their per-user state
// (mailbox, skydrive, ...) without depending on the device having synced.
public record AccountCreatedEvent(string UserId, string Email, string Username);

// broadcast when an account is deleted, so backends can tear down their per-user state
// (mailbox, skydrive, ...). Irreversible on the receiving end.
public record AccountDeletedEvent(string UserId);

public static class AccountEvents
{
    public static readonly RedisChannel Created = RedisChannel.Literal("account.created");
    public static readonly RedisChannel Deleted = RedisChannel.Literal("account.deleted");

    public static Task PublishCreatedAsync(this IConnectionMultiplexer redis, AccountCreatedEvent evt) =>
        redis.GetSubscriber().PublishAsync(Created, JsonSerializer.SerializeToUtf8Bytes(evt));

    public static Task PublishDeletedAsync(this IConnectionMultiplexer redis, AccountDeletedEvent evt) =>
        redis.GetSubscriber().PublishAsync(Deleted, JsonSerializer.SerializeToUtf8Bytes(evt));
}
