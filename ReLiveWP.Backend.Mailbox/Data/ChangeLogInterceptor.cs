using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ReLiveWP.Backend.Mailbox.Data.Entities;

namespace ReLiveWP.Backend.Mailbox.Data;

// Automatically emits FolderEvent / ItemEvent rows whenever entities are
// saved, so callers never need to hand-write event rows.
//
// Runs inside the same SaveChanges call — events are committed atomically
// with the data change. Registered via AddDbContext interceptor option.
public sealed class ChangeLogInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        EmitEvents((MailboxDbContext)eventData.Context!);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        EmitEvents((MailboxDbContext)eventData.Context!);
        return base.SavingChanges(eventData, result);
    }

    private static void EmitEvents(MailboxDbContext db)
    {
        var now = DateTime.UtcNow;
        var entries = db.ChangeTracker.Entries().ToList();

        // Collect server IDs of items that need an Update event because a child
        // row changed but the parent item itself wasn't directly modified.
        var childItemServerIds = new HashSet<string>();

        foreach (var entry in entries)
        {
            switch (entry.Entity)
            {
                //
                // items
                case DbItem item when entry.State == EntityState.Added:
                    db.ItemEvents.Add(new DbItemEvent
                    {
                        UserId = item.UserId,
                        CollectionId = item.CollectionId,
                        EventType = DbChangeEventType.Add,
                        ServerId = item.ServerId,
                        OccurredAt = now,
                    });
                    break;

                case DbItem item when entry.State == EntityState.Modified:
                    if (IsSoftDelete(entry))
                    {
                        db.ItemEvents.Add(new DbItemEvent
                        {
                            UserId = item.UserId,
                            CollectionId = item.CollectionId,
                            EventType = DbChangeEventType.Delete,
                            ServerId = item.ServerId,
                            OccurredAt = now,
                        });
                    }
                    else
                    {
                        db.ItemEvents.Add(new DbItemEvent
                        {
                            UserId = item.UserId,
                            CollectionId = item.CollectionId,
                            EventType = DbChangeEventType.Update,
                            ServerId = item.ServerId,
                            OccurredAt = now,
                        });
                    }
                    break;

                //
                // folders
                case DbFolder folder when entry.State == EntityState.Added:
                    db.FolderEvents.Add(new DbFolderEvent
                    {
                        UserId = folder.UserId,
                        EventType = DbChangeEventType.Add,
                        ServerId = folder.Id,
                        ParentServerId = folder.ParentServerId,
                        DisplayName = folder.DisplayName,
                        FolderType = folder.Type,
                        OccurredAt = now,
                    });
                    break;

                case DbFolder folder when entry.State == EntityState.Modified:
                    if (IsSoftDelete(entry))
                    {
                        db.FolderEvents.Add(new DbFolderEvent
                        {
                            UserId = folder.UserId,
                            EventType = DbChangeEventType.Delete,
                            ServerId = folder.Id,
                            OccurredAt = now,
                        });
                    }
                    else
                    {
                        db.FolderEvents.Add(new DbFolderEvent
                        {
                            UserId = folder.UserId,
                            EventType = DbChangeEventType.Update,
                            ServerId = folder.Id,
                            ParentServerId = folder.ParentServerId,
                            DisplayName = folder.DisplayName,
                            FolderType = folder.Type,
                            OccurredAt = now,
                        });
                    }
                    break;

                //
                // contact children
                case DbContactCategory c when IsChildMutation(entry):
                    TryAddChildUpdate(db, c.ContactItemId, childItemServerIds);
                    break;
                case DbContactChild c when IsChildMutation(entry):
                    TryAddChildUpdate(db, c.ContactItemId, childItemServerIds);
                    break;
                case DbContactAnnotation a when IsChildMutation(entry):
                    TryAddChildUpdate(db, a.ContactItemId, childItemServerIds);
                    break;

                //
                // calendar children
                case DbCalendarAttendee a when IsChildMutation(entry):
                    TryAddChildUpdate(db, a.CalendarItemId, childItemServerIds);
                    break;
                case DbCalendarCategory c when IsChildMutation(entry):
                    TryAddChildUpdate(db, c.CalendarItemId, childItemServerIds);
                    break;
                case DbCalendarException ex when IsChildMutation(entry):
                    TryAddChildUpdate(db, ex.CalendarItemId, childItemServerIds);
                    break;

                //
                // nested calendar children
                case DbCalendarExceptionAttendee ea when IsChildMutation(entry):
                    TryBubbleThroughException(db, ea.CalendarExceptionId, childItemServerIds);
                    break;
                case DbCalendarExceptionCategory ec when IsChildMutation(entry):
                    TryBubbleThroughException(db, ec.CalendarExceptionId, childItemServerIds);
                    break;
            }
        }

        // Emit Update events for items touched only via child rows.
        // Skip any item whose parent was already directly modified above
        // (those already have their own event row).
        var alreadyHandled = entries
            .Where(e => e.Entity is DbItem && e.State is EntityState.Added or EntityState.Modified)
            .Select(e => ((DbItem)e.Entity).ServerId)
            .ToHashSet();

        foreach (var serverId in childItemServerIds)
        {
            if (alreadyHandled.Contains(serverId)) continue;

            var item = db.Items.Local.FirstOrDefault(i => i.ServerId == serverId);
            if (item is null) continue;

            db.ItemEvents.Add(new DbItemEvent
            {
                UserId = item.UserId,
                CollectionId = item.CollectionId,
                EventType = DbChangeEventType.Update,
                ServerId = serverId,
                OccurredAt = now,
            });
        }
    }

    // True when a child entity was added, modified, or deleted in this batch.
    private static bool IsChildMutation(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry) =>
        entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;

    // Soft-delete: DeletedAt changed from null to a value.
    private static bool IsSoftDelete(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "DeletedAt");
        return prop is { IsModified: true } && prop.OriginalValue is null && prop.CurrentValue is not null;
    }

    // Resolve the parent item's ServerId from its Id (PK = same as ServerId by convention).
    // Checks the Local identity map first to avoid a round-trip.
    private static void TryAddChildUpdate(MailboxDbContext db, string itemId, HashSet<string> target)
    {
        var item = db.Items.Local.FirstOrDefault(i => i.Id == itemId);
        if (item is not null)
            target.Add(item.ServerId);
    }

    // For exception children: look up the exception in the Local map, then bubble to its owner.
    private static void TryBubbleThroughException(MailboxDbContext db, string exceptionId, HashSet<string> target)
    {
        var ex = db.CalendarExceptions.Local.FirstOrDefault(e => e.Id == exceptionId);
        if (ex is not null)
            TryAddChildUpdate(db, ex.CalendarItemId, target);
    }
}
