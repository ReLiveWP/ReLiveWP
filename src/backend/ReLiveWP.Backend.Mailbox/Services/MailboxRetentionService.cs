using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;

namespace ReLiveWP.Backend.Mailbox.Services;

public record struct RetentionSweepResult(int Items, int Folders, int ItemEvents, int FolderEvents);

// hard-deletes soft-deleted items/folders past retention, and prunes change-log rows a device
// can no longer need.
public class MailboxRetentionSweepService(MailboxDbContext db)
{
    public async Task<RetentionSweepResult> SweepAsync(TimeSpan retentionWindow, CancellationToken ct = default)
    {
        var (items, folders) = await PurgeSoftDeletedAsync(retentionWindow, ct);
        var (itemEvents, folderEvents) = await PruneChangeLogAsync(ct);
        return new RetentionSweepResult(items, folders, itemEvents, folderEvents);
    }

    private async Task<(int Items, int Folders)> PurgeSoftDeletedAsync(TimeSpan retentionWindow, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - retentionWindow;
        var items = await db.Items.Where(i => i.DeletedAt != null && i.DeletedAt < cutoff).ExecuteDeleteAsync(ct);
        var folders = await db.Folders.Where(f => f.DeletedAt != null && f.DeletedAt < cutoff).ExecuteDeleteAsync(ct);
        return (items, folders);
    }

    private async Task<(int ItemEvents, int FolderEvents)> PruneChangeLogAsync(CancellationToken ct)
    {
        var itemFloors = await db.SyncStates
            .Where(s => s.CollectionId != DbSyncState.FolderHierarchyCollectionId)
            .GroupBy(s => s.CollectionId)
            .Select(g => new
            {
                CollectionId = g.Key,
                Floor = g.Min(s => s.Watermark < s.PreviousWatermark ? s.Watermark : s.PreviousWatermark),
            })
            .ToListAsync(ct);

        var itemEventsDeleted = 0;
        foreach (var f in itemFloors)
            itemEventsDeleted += await db.ItemEvents
                .Where(e => e.CollectionId == f.CollectionId && e.CommitId < f.Floor)
                .ExecuteDeleteAsync(ct);

        var folderFloors = await db.SyncStates
            .Where(s => s.CollectionId == DbSyncState.FolderHierarchyCollectionId)
            .GroupBy(s => s.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Floor = g.Min(s => s.Watermark < s.PreviousWatermark ? s.Watermark : s.PreviousWatermark),
            })
            .ToListAsync(ct);

        var folderEventsDeleted = 0;
        foreach (var f in folderFloors)
            folderEventsDeleted += await db.FolderEvents
                .Where(e => e.UserId == f.UserId && e.CommitId < f.Floor)
                .ExecuteDeleteAsync(ct);

        return (itemEventsDeleted, folderEventsDeleted);
    }
}

public class MailboxRetentionService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<MailboxRetentionService> logger) : BackgroundService
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan DefaultRetentionWindow = TimeSpan.FromDays(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = configuration.GetValue("Mailbox:Retention:Interval", DefaultInterval);
        var retentionWindow = configuration.GetValue("Mailbox:Retention:Window", DefaultRetentionWindow);
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sweeper = scope.ServiceProvider.GetRequiredService<MailboxRetentionSweepService>();
                var result = await sweeper.SweepAsync(retentionWindow, stoppingToken);

                if (result.Items > 0 || result.Folders > 0 || result.ItemEvents > 0 || result.FolderEvents > 0)
                    logger.LogInformation(
                        "retention sweep: purged {Items} item(s), {Folders} folder(s), {ItemEvents} item-event(s), {FolderEvents} folder-event(s)",
                        result.Items, result.Folders, result.ItemEvents, result.FolderEvents);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "retention sweep failed; will retry next interval");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
