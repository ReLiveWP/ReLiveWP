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

                // the mirror made this folder, so unlinking with the data has to take it too or
                // relinking stacks up a duplicate beside it every time
                await DeleteFolderAsync(source, ct);
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

    private async Task DeleteFolderAsync(DbSyncSource source, CancellationToken ct)
    {
        if (source.FolderId is not { Length: > 0 } folderId) return;

        try
        {
            await mailbox.DeleteFolderAsync(new DeleteFolderRequest
            {
                UserId = source.UserId,
                ServerId = folderId,
            }, cancellationToken: ct);

            logger.LogInformation("removed the {Kind} folder {Folder} for {Service}/{Source}",
                source.Kind, folderId, source.ServiceId, source.SourceId);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "could not remove the folder {Folder} for {Service}/{Source}",
                folderId, source.ServiceId, source.SourceId);
        }
    }

    public Task<List<DbSyncSource>> ForConnectionAsync(
        string userId, string connectionId, CancellationToken ct = default) =>
        db.SyncSources
            .Where(s => s.UserId == userId && s.ConnectionId == connectionId)
            .ToListAsync(ct);
}
