using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data.Entities;

namespace ReLiveWP.Backend.Mailbox.Data;

public static class BulkItemDeletion
{
    private const int UpdateChunkSize = 1000;

    // soft-deletes without tracking a single item: one projection, chunked UPDATEs, then the change
    // events the interceptors would otherwise have derived from tracked entities. joins an ambient
    // transaction when the caller opened one, otherwise runs in its own.
    public static async Task<int> SoftDeleteItemsAsync(this MailboxDbContext db, IQueryable<DbItem> targeted, CancellationToken ct)
    {
        var doomed = await targeted
            .Where(i => i.DeletedAt == null)
            .Select(i => new DoomedItem(i.Id, i.UserId, i.CollectionId, i.ServerId))
            .ToListAsync(ct);
        if (doomed.Count == 0)
            return 0;

        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction ? await db.Database.BeginTransactionAsync(ct) : null;

        var now = DateTime.UtcNow;
        var deleted = 0;
        foreach (var chunk in doomed.Chunk(UpdateChunkSize))
        {
            var ids = chunk.Select(d => d.Id).ToList();
            deleted += await db.Items
                .Where(i => ids.Contains(i.Id) && i.DeletedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.DeletedAt, now), ct);
        }

        var commitId = ChangeEventCursor.NextCommitId(db.Database);
        db.ItemEvents.AddRange(doomed.Select(d => new DbItemEvent
        {
            CommitId = commitId,
            UserId = d.UserId,
            CollectionId = d.CollectionId,
            EventType = DbChangeEventType.Delete,
            ServerId = d.ServerId,
            OccurredAt = now,
        }));
        await db.SaveChangesAsync(ct);

        if (transaction is not null)
            await transaction.CommitAsync(ct);

        return deleted;
    }

    private sealed record DoomedItem(string Id, string UserId, string CollectionId, string ServerId);
}
