using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Services.Push.Data;

public class NotificationQueue(IDbContextFactory<PushDatabase> dbFactory)
{
    public async Task<long> EnqueueAsync(
        string deviceId,
        long channelId,
        byte[] payload,
        uint notificationClass,
        DateTimeOffset? expiresAt = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var item = new QueuedNotification
        {
            DeviceId = deviceId,
            ChannelId = channelId,
            Payload = payload,
            NotificationClass = notificationClass,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ExpiresAt = expiresAt?.ToUnixTimeMilliseconds(),
        };

        db.Notifications.Add(item);
        await db.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task<IReadOnlyList<QueuedNotification>> ClaimForDeviceAsync(
        string deviceId,
        TimeSpan leaseDuration,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // so this is a bit funky, basically we need to make sure the database knows that we own
        // these notifications so they don't get touched by anybody else (in the event we have 2 things
        // tryina pull notifications at once)

        // in the ideal case, this isn't super important because we'll just delete these notifications
        // when they get pushed to a device anyway, but we can't always trust that's what'll happen

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var leaseOwner = Guid.NewGuid().ToString("N");
        var leaseUntil = now + (long)leaseDuration.TotalMilliseconds;

        await db.Notifications
            .Where(n => n.DeviceId == deviceId
                     && n.Status == NotificationStatus.Pending
                     && (n.ExpiresAt == null || n.ExpiresAt > now))
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.Status, NotificationStatus.InFlight)
                .SetProperty(n => n.LeaseOwner, leaseOwner)
                .SetProperty(n => n.LeaseExpiresAt, leaseUntil), ct);

        // read back only the rows we just claimed (by owner), in order
        return await db.Notifications
            .Where(n => n.LeaseOwner == leaseOwner && n.Status == NotificationStatus.InFlight)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task CompleteAsync(long id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.Notifications.Where(n => n.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task FailAsync(long id, string error, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.Notifications
            .Where(n => n.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.Status, NotificationStatus.Pending)
                .SetProperty(n => n.LeaseOwner, (string)null)
                .SetProperty(n => n.LeaseExpiresAt, (long?)null)
                .SetProperty(n => n.AttemptCount, n => n.AttemptCount + 1)
                .SetProperty(n => n.LastError, error), ct);
    }

    public async Task<int> ReclaimExpiredAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return await db.Notifications
            .Where(n => n.Status == NotificationStatus.InFlight
                     && n.LeaseExpiresAt != null && n.LeaseExpiresAt < now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.Status, NotificationStatus.Pending)
                .SetProperty(n => n.LeaseOwner, (string)null)
                .SetProperty(n => n.LeaseExpiresAt, (long?)null)
                .SetProperty(n => n.AttemptCount, n => n.AttemptCount + 1), ct);
    }

    public async Task<int> ExpireAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return await db.Notifications
            .Where(n => n.ExpiresAt != null && n.ExpiresAt < now
                     && n.Status != NotificationStatus.InFlight)
            .ExecuteDeleteAsync(ct);
    }
}
