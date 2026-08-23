using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ClearingHouse.Data;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror;

public class SyncSourceDetach(
    MailboxStore.MailboxStoreClient mailbox,
    ClearingHouseDbContext db,
    ILogger<SyncSourceDetach> logger)
{
    public async Task<int> DetachAsync(IReadOnlyList<DbSyncSource> sources, CancellationToken ct = default)
    {
        if (sources.Count == 0) return 0;

        foreach (var source in sources)
        {
            db.SyncSources.Remove(source);

            logger.LogInformation("detached {Kind} {Service}/{Source} for {User}; its items stay",
                source.Kind, source.ServiceId, source.SourceId, source.UserId);
        }

        await db.SaveChangesAsync(ct);
        return sources.Count;
    }

    public async Task<int> DeleteAndDetachAsync(
        IReadOnlyList<DbSyncSource> sources, CancellationToken ct = default)
    {
        var deleted = 0;

        foreach (var source in sources)
        {
            try
            {
                var result = await mailbox.DeleteItemsByOriginAsync(new DeleteItemsByOriginRequest
                {
                    UserId = source.UserId,
                    OriginServiceId = source.ServiceId,
                    OriginCollectionId = source.SourceId,
                }, cancellationToken: ct);

                deleted += result.ItemsDeleted;

                logger.LogInformation("removed {Count} {Kind} item(s) from {Service}/{Source} for {User}",
                    result.ItemsDeleted, source.Kind, source.ServiceId, source.SourceId, source.UserId);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogWarning(e, "could not remove items for {Service}/{Source}; detaching regardless",
                    source.ServiceId, source.SourceId);
            }
        }

        await DetachAsync(sources, ct);
        return deleted;
    }

    public Task<List<DbSyncSource>> ForConnectionAsync(
        string userId, string connectionId, CancellationToken ct = default) =>
        db.SyncSources
            .Where(s => s.UserId == userId && s.ConnectionId == connectionId)
            .ToListAsync(ct);
}
