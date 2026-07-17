using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;

namespace ReLiveWP.Backend.Mailbox.Services;

public class SyncStateRepairService(MailboxDbContext db)
{
    private const int PageSize = 500;
    private const string FolderHierarchyCollectionId = "0"; // sentinel; never corresponds to a DbFolder row

    // deletes sync states for folders that have been deleted
    public async Task<int> SweepAsync(string? userId, CancellationToken ct)
    {
        var states = await RemoveFolderOrphansAsync(userId, ct);
        var deviceStates = await RemoveDeviceOrphansAsync(userId, ct);
        return states + deviceStates;
    }

    private async Task<int> RemoveFolderOrphansAsync(string? userId, CancellationToken ct)
    {
        int statesDeleted = 0;
        int offset = 0;

        while (true)
        {
            var query = db.SyncStates.Where(s => s.CollectionId != FolderHierarchyCollectionId);
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(s => s.UserId == userId);

            var pairs = await query
                .Select(s => new { s.UserId, s.CollectionId })
                .Distinct()
                .OrderBy(p => p.UserId).ThenBy(p => p.CollectionId)
                .Skip(offset).Take(PageSize)
                .ToListAsync(ct);

            if (pairs.Count == 0) break;
            offset += pairs.Count;

            var byUser = pairs.ToLookup(p => p.UserId);
            var liveFolderIds = new HashSet<(string UserId, string CollectionId)>();
            foreach (var userGroup in byUser)
            {
                var collectionIds = userGroup.Select(p => p.CollectionId).ToList();
                var live = await db.Folders
                    .Where(f => f.UserId == userGroup.Key && f.DeletedAt == null
                             && collectionIds.Contains(f.Id))
                    .Select(f => f.Id)
                    .ToListAsync(ct);

                foreach (var id in live)
                    liveFolderIds.Add((userGroup.Key, id));
            }

            var orphanedPairs = pairs
                .Where(p => !liveFolderIds.Contains((p.UserId, p.CollectionId)))
                .ToList();
            if (orphanedPairs.Count == 0) continue;

            foreach (var pair in orphanedPairs)
            {
                var states = await db.SyncStates
                    .Where(s => s.UserId == pair.UserId && s.CollectionId == pair.CollectionId)
                    .ToListAsync(ct);

                db.SyncStates.RemoveRange(states);
                statesDeleted += states.Count;
            }

            await db.SaveChangesAsync(ct);
        }

        return statesDeleted;
    }

    // currently unreachable: nothing deletes a DbDeviceInfo row yet, kept ready for when something does
    private async Task<int> RemoveDeviceOrphansAsync(string? userId, CancellationToken ct)
    {
        var stateQuery = db.SyncStates.AsQueryable();
        if (!string.IsNullOrEmpty(userId)) stateQuery = stateQuery.Where(s => s.UserId == userId);

        var devicePairs = await stateQuery
            .Select(s => new { s.UserId, s.DeviceId })
            .Distinct()
            .ToListAsync(ct);

        int states = 0;
        foreach (var pair in devicePairs)
        {
            var known = await db.DeviceInfos.AnyAsync(
                d => d.UserId == pair.UserId && d.DeviceId == pair.DeviceId, ct);
            if (known) continue;

            var orphanStates = await db.SyncStates
                .Where(s => s.UserId == pair.UserId && s.DeviceId == pair.DeviceId)
                .ToListAsync(ct);

            db.SyncStates.RemoveRange(orphanStates);
            states += orphanStates.Count;
        }

        if (states > 0)
            await db.SaveChangesAsync(ct);

        return states;
    }
}
