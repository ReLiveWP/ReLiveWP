using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.ServiceDefaults.Events;
using StackExchange.Redis;

namespace ReLiveWP.Backend.Mailbox.Data;

// records change log entries (for activesync) and emits redis pubsub events (for push) on every save
public sealed class ChangeLogInterceptor : SaveChangesInterceptor
{
    private const string FolderHierarchyCollectionId = "0";

    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<ChangeLogInterceptor> _logger;

    public ChangeLogInterceptor() : this(null, null) { }
    public ChangeLogInterceptor(IConnectionMultiplexer redis) : this(redis, null) { }
    public ChangeLogInterceptor(IConnectionMultiplexer? redis, ILogger<ChangeLogInterceptor>? logger)
    {
        _redis = redis;
        _logger = logger ?? NullLogger<ChangeLogInterceptor>.Instance;
    }

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

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken ct = default)
    {
        await PublishNotificationsAsync((MailboxDbContext)eventData.Context!);
        return await base.SavedChangesAsync(eventData, result, ct);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        var db = (MailboxDbContext)eventData.Context!;
        _ = PublishNotificationsAsync(db).ContinueWith(
            t => _logger.LogError(t.Exception, "failed publishing mailbox.changed notifications"),
            CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return base.SavedChanges(eventData, result);
    }

    private async Task PublishNotificationsAsync(MailboxDbContext db)
    {
        if (db.PendingChangeNotifications.Count == 0)
            return;

        var pending = db.PendingChangeNotifications.ToArray();
        db.PendingChangeNotifications.Clear();

        if (_redis is null)
            return;

        foreach (var evt in pending)
            await _redis.PublishChangedAsync(evt);
    }

    private static MailboxChangeKind ToKind(DbChangeEventType t) => t switch
    {
        DbChangeEventType.Add => MailboxChangeKind.Add,
        DbChangeEventType.Delete => MailboxChangeKind.Delete,
        _ => MailboxChangeKind.Update,
    };

    private static long fallbackCommitId = ChangeEventCursor.CommitIdOffset;

    private static void EmitEvents(MailboxDbContext db)
    {
        var now = DateTime.UtcNow;
        var commitId = db.Database.IsNpgsql() ? 0L : Interlocked.Increment(ref fallbackCommitId);
        var entries = db.ChangeTracker.Entries().ToList();
        var childItemServerIds = new HashSet<string>();

        foreach (var entry in entries)
        {
            switch (entry.Entity)
            {
                case DbItem item when entry.State == EntityState.Added:
                    db.ItemEvents.Add(new DbItemEvent
                    {
                        CommitId = commitId,
                        UserId = item.UserId,
                        CollectionId = item.CollectionId,
                        EventType = DbChangeEventType.Add,
                        ServerId = item.ServerId,
                        OccurredAt = now,
                    });
                    break;

                case DbItem item when entry.State == EntityState.Modified:
                    // a move is a delete from the old collection plus an add to the new one
                    var collectionProp = entry.Property(nameof(DbItem.CollectionId));
                    if (collectionProp.IsModified &&
                        (string?)collectionProp.OriginalValue != (string?)collectionProp.CurrentValue)
                    {
                        db.ItemEvents.Add(new DbItemEvent
                        {
                            CommitId = commitId,
                            UserId = item.UserId,
                            CollectionId = (string)collectionProp.OriginalValue!,
                            EventType = DbChangeEventType.Delete,
                            ServerId = item.ServerId,
                            OccurredAt = now,
                        });
                        db.ItemEvents.Add(new DbItemEvent
                        {
                            CommitId = commitId,
                            UserId = item.UserId,
                            CollectionId = (string)collectionProp.CurrentValue!,
                            EventType = DbChangeEventType.Add,
                            ServerId = item.ServerId,
                            OccurredAt = now,
                        });
                        break;
                    }

                    if (IsSoftDelete(entry))
                    {
                        db.ItemEvents.Add(new DbItemEvent
                        {
                            CommitId = commitId,
                            UserId = item.UserId,
                            CollectionId = item.CollectionId,
                            EventType = DbChangeEventType.Delete,
                            ServerId = item.ServerId,
                            OccurredAt = now,
                        });
                    }
                    else if (IsValidationFlagTransition(entry, out var becameFlagged))
                    {
                        // delete items that become invalid, add them back when fixed
                        db.ItemEvents.Add(new DbItemEvent
                        {
                            CommitId = commitId,
                            UserId = item.UserId,
                            CollectionId = item.CollectionId,
                            EventType = becameFlagged ? DbChangeEventType.Delete : DbChangeEventType.Add,
                            ServerId = item.ServerId,
                            OccurredAt = now,
                        });
                    }
                    else
                    {
                        db.ItemEvents.Add(new DbItemEvent
                        {
                            CommitId = commitId,
                            UserId = item.UserId,
                            CollectionId = item.CollectionId,
                            EventType = DbChangeEventType.Update,
                            ServerId = item.ServerId,
                            OccurredAt = now,
                        });
                    }
                    break;

                case DbFolder folder when entry.State == EntityState.Added:
                    db.FolderEvents.Add(new DbFolderEvent
                    {
                        CommitId = commitId,
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
                            CommitId = commitId,
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
                            CommitId = commitId,
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

                case DbContactCategory c when IsChildMutation(entry):
                    TryAddChildUpdate(db, c.ContactItemId, childItemServerIds);
                    break;
                case DbContactChild c when IsChildMutation(entry):
                    TryAddChildUpdate(db, c.ContactItemId, childItemServerIds);
                    break;
                case DbContactAnnotation a when IsChildMutation(entry):
                    TryAddChildUpdate(db, a.ContactItemId, childItemServerIds);
                    break;

                case DbCalendarAttendee a when IsChildMutation(entry):
                    TryAddChildUpdate(db, a.CalendarItemId, childItemServerIds);
                    break;
                case DbCalendarCategory c when IsChildMutation(entry):
                    TryAddChildUpdate(db, c.CalendarItemId, childItemServerIds);
                    break;
                case DbCalendarException ex when IsChildMutation(entry):
                    TryAddChildUpdate(db, ex.CalendarItemId, childItemServerIds);
                    break;

                case DbCalendarExceptionAttendee ea when IsChildMutation(entry):
                    TryBubbleThroughException(db, ea.CalendarExceptionId, childItemServerIds);
                    break;
                case DbCalendarExceptionCategory ec when IsChildMutation(entry):
                    TryBubbleThroughException(db, ec.CalendarExceptionId, childItemServerIds);
                    break;
            }
        }

        // items only modified through children still need an update event
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
                CommitId = commitId,
                UserId = item.UserId,
                CollectionId = item.CollectionId,
                EventType = DbChangeEventType.Update,
                ServerId = serverId,
                OccurredAt = now,
            });
        }

        // deliver push notifications after the save completes, not before (avoids a race)
        foreach (var e in db.ChangeTracker.Entries<DbItemEvent>())
            if (e.State == EntityState.Added)
                db.PendingChangeNotifications.Add(new MailboxChangedEvent(
                    e.Entity.UserId, e.Entity.CollectionId, e.Entity.ServerId, ToKind(e.Entity.EventType)));

        foreach (var e in db.ChangeTracker.Entries<DbFolderEvent>())
            if (e.State == EntityState.Added)
                db.PendingChangeNotifications.Add(new MailboxChangedEvent(
                    e.Entity.UserId, FolderHierarchyCollectionId, e.Entity.ServerId, ToKind(e.Entity.EventType)));
    }

    private static bool IsChildMutation(EntityEntry entry) =>
        entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;

    private static bool IsSoftDelete(EntityEntry entry)
    {
        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "DeletedAt");
        return prop is { IsModified: true } && prop.OriginalValue is null && prop.CurrentValue is not null;
    }

    private static bool IsValidationFlagTransition(EntityEntry entry, out bool becameFlagged)
    {
        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(DbItem.ValidationFlaggedAt));
        if (prop is { IsModified: true } && (prop.OriginalValue is null) != (prop.CurrentValue is null))
        {
            becameFlagged = prop.CurrentValue is not null;
            return true;
        }
        becameFlagged = false;
        return false;
    }

    private static void TryAddChildUpdate(MailboxDbContext db, string itemId, HashSet<string> target)
    {
        var item = db.Items.Local.FirstOrDefault(i => i.Id == itemId);
        if (item is not null)
            target.Add(item.ServerId);
    }

    private static void TryBubbleThroughException(MailboxDbContext db, string exceptionId, HashSet<string> target)
    {
        var ex = db.CalendarExceptions.Local.FirstOrDefault(e => e.Id == exceptionId);
        if (ex is not null)
            TryAddChildUpdate(db, ex.CalendarItemId, target);
    }
}
