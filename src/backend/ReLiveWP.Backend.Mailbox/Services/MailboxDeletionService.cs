using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;

namespace ReLiveWP.Backend.Mailbox.Services;

public class MailboxDeletionService(MailboxDbContext db)
{
    public async Task<MailboxDeletionResult> DeleteAsync(string userId, CancellationToken ct = default)
    {
        // hard-deletes every row belonging to a user
        var itemsDeleted = await db.Items.Where(i => i.UserId == userId).ExecuteDeleteAsync(ct);
        var foldersDeleted = await db.Folders.Where(f => f.UserId == userId).ExecuteDeleteAsync(ct);

        await db.SyncStates.Where(s => s.UserId == userId).ExecuteDeleteAsync(ct);
        await db.DeviceInfos.Where(d => d.UserId == userId).ExecuteDeleteAsync(ct);
        await db.ContactIdentities.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.ItemEvents.Where(e => e.UserId == userId).ExecuteDeleteAsync(ct);
        await db.FolderEvents.Where(e => e.UserId == userId).ExecuteDeleteAsync(ct);

        return new MailboxDeletionResult(foldersDeleted, itemsDeleted);
    }
}

public record struct MailboxDeletionResult(int FoldersDeleted, int ItemsDeleted);
