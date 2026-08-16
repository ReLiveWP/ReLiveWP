using System.Text.Json;
using StackExchange.Redis;

namespace ReLiveWP.ServiceDefaults.Events;

// broadcast when a non-device account is created, so backends can spin up their per-user state
public record AccountCreatedEvent(string UserId, string Email, string Username);

// broadcast when an account is deleted, so backends can delete down their per-user state
public record AccountDeletedEvent(string UserId);

// broadcast when the owner's profile changes, so backends can mirror it
public record AccountProfileChangedEvent(
    string UserId,
    string Cid,
    string? FirstName,
    string? LastName,
    string Username,
    string Email,
    string? PictureEtag,
    string Visibility = "Private");

// broadcast when a connected account is unlinked, so backends can drop whatever it was feeding.
public record ConnectionDeletedEvent(string UserId, string ConnectionId, string Service, bool DeleteData);

public static class AccountEvents
{
    public static readonly RedisChannel Created = RedisChannel.Literal("account.created");
    public static readonly RedisChannel Deleted = RedisChannel.Literal("account.deleted");
    public static readonly RedisChannel ProfileChanged = RedisChannel.Literal("account.profile-changed");
    public static readonly RedisChannel ConnectionDeleted = RedisChannel.Literal("connection.deleted");

    public static Task PublishConnectionDeletedAsync(this IConnectionMultiplexer redis, ConnectionDeletedEvent evt) =>
        redis.GetSubscriber().PublishAsync(ConnectionDeleted, JsonSerializer.SerializeToUtf8Bytes(evt));

    public static Task PublishCreatedAsync(this IConnectionMultiplexer redis, AccountCreatedEvent evt) =>
        redis.GetSubscriber().PublishAsync(Created, JsonSerializer.SerializeToUtf8Bytes(evt));

    public static Task PublishDeletedAsync(this IConnectionMultiplexer redis, AccountDeletedEvent evt) =>
        redis.GetSubscriber().PublishAsync(Deleted, JsonSerializer.SerializeToUtf8Bytes(evt));

    public static Task PublishProfileChangedAsync(this IConnectionMultiplexer redis, AccountProfileChangedEvent evt) =>
        redis.GetSubscriber().PublishAsync(ProfileChanged, JsonSerializer.SerializeToUtf8Bytes(evt));
}
