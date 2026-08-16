using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ClearingHouse.Data;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public class ContactSourceDetach(
    MailboxStore.MailboxStoreClient mailbox,
    ClearingHouseDbContext db,
    ILogger<ContactSourceDetach> logger)
{
    public async Task<int> DetachAsync(IReadOnlyList<DbContactSyncSource> sources, CancellationToken ct = default)
    {
        if (sources.Count == 0) return 0;

        foreach (var source in sources)
        {
            db.ContactSyncSources.Remove(source);

            logger.LogInformation("detached {Service}/{Source} for {User}; its contacts stay",
                source.ServiceId, source.SourceId, source.UserId);
        }

        await db.SaveChangesAsync(ct);
        return sources.Count;
    }

    public async Task<int> DeleteAndDetachAsync(
        IReadOnlyList<DbContactSyncSource> sources, CancellationToken ct = default)
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

                logger.LogInformation("removed {Count} contact(s) from {Service}/{Source} for {User}",
                    result.ItemsDeleted, source.ServiceId, source.SourceId, source.UserId);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogWarning(e, "could not remove contacts for {Service}/{Source}; detaching regardless",
                    source.ServiceId, source.SourceId);
            }
        }

        await DetachAsync(sources, ct);
        return deleted;
    }

    public Task<List<DbContactSyncSource>> ForConnectionAsync(
        string userId, string connectionId, CancellationToken ct = default) =>
        db.ContactSyncSources
            .Where(s => s.UserId == userId && s.ConnectionId == connectionId)
            .ToListAsync(ct);
}
