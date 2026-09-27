using System.Text.Json;
using StackExchange.Redis;

namespace ReLiveWP.ServiceDefaults.Events;

public enum MailboxChangeKind { Add, Update, Delete }

// broadcast whenever an item or folder changes in a user's mailbox, so the EAS front end can
// wake held Ping / heartbeat-Sync connections. CollectionId is the folder hierarchy id ("0")
// for folder events, otherwise the item's collection.
public record MailboxChangedEvent(string UserId, string CollectionId, string ServerId, MailboxChangeKind Kind);

public static class MailboxEvents
{
    public static readonly RedisChannel Changed = RedisChannel.Literal("mailbox.changed");

    public static Task PublishChangedAsync(this IConnectionMultiplexer redis, MailboxChangedEvent evt) =>
        redis.GetSubscriber().PublishAsync(Changed, JsonSerializer.SerializeToUtf8Bytes(evt));
}
