using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.ServiceDefaults.Events;
using StackExchange.Redis;

namespace ReLiveWP.Backend.Mailbox.Data;

// records change log entries (for activesync) and emits redis pubsub events (for push) on every save
public sealed class ChangeLogInterceptor : SaveChangesInterceptor, IDbTransactionInterceptor
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

    // inside an explicit transaction the save isn't visible yet, so the publish waits for the commit
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken ct = default)
    {
        var db = (MailboxDbContext)eventData.Context!;
        if (db.Database.CurrentTransaction is null)
            await PublishNotificationsAsync(db);
        return await base.SavedChangesAsync(eventData, result, ct);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        var db = (MailboxDbContext)eventData.Context!;
        if (db.Database.CurrentTransaction is null)
            _ = PublishNotificationsAsync(db);
        return base.SavedChanges(eventData, result);
    }

    public Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken ct = default) =>
        PublishNotificationsAsync((MailboxDbContext)eventData.Context!);

    public void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData) =>
        _ = PublishNotificationsAsync((MailboxDbContext)eventData.Context!);

    public Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken ct = default)
    {
        ((MailboxDbContext)eventData.Context!).PendingChangeNotifications.Clear();
        return Task.CompletedTask;
    }

    public void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData) =>
        ((MailboxDbContext)eventData.Context!).PendingChangeNotifications.Clear();

    // the save has already committed by the time this runs, so a publish failure is a device that
    // waits for its next poll, never a failed save. the listener only keys on (user, collection),
    // so one publish per collection is all a save needs however many items it touched
    private async Task PublishNotificationsAsync(MailboxDbContext db)
    {
        if (db.PendingChangeNotifications.Count == 0)
            return;

        var pending = db.PendingChangeNotifications.DistinctBy(e => (e.UserId, e.CollectionId)).ToArray();
        db.PendingChangeNotifications.Clear();

        if (_redis is null)
            return;

        foreach (var evt in pending)
        {
            try
            {
                await _redis.PublishChangedAsync(evt);
            }
            catch (Exception ex)
            {
                MailboxMetrics.PublishFailures.Add(1);
                _logger.LogError(ex, "failed publishing mailbox.changed for collection {Collection}", evt.CollectionId);
            }
        }
    }

    private static MailboxChangeKind ToKind(DbChangeEventType t) => t switch
    {
        DbChangeEventType.Add => MailboxChangeKind.Add,
        DbChangeEventType.Delete => MailboxChangeKind.Delete,
        _ => MailboxChangeKind.Update,
    };

    private static void EmitEvents(MailboxDbContext db)
    {
        var now = DateTime.UtcNow;
        var commitId = ChangeEventCursor.NextCommitId(db.Database);
        var entries = db.ChangeTracker.Entries().ToList();
        var index = new TrackedItemIndex(db);
        var childTouched = new HashSet<DbItem>(ReferenceEqualityComparer.Instance);

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

                default:
                    if (TrackedItemIndex.IsChildMutation(entry) && index.ParentItemOf(entry.Entity) is { } parent)
                        childTouched.Add(parent);
                    break;
            }
        }

        // items only modified through children still need an update event
        var alreadyHandled = entries
            .Where(e => e.Entity is DbItem && e.State is EntityState.Added or EntityState.Modified)
            .Select(e => ((DbItem)e.Entity).ServerId)
            .ToHashSet();

        foreach (var item in childTouched)
        {
            if (alreadyHandled.Contains(item.ServerId)) continue;

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

        // deliver push notifications after the save completes, not before (avoids a race)
        foreach (var e in db.ChangeTracker.Entries<DbItemEvent>())
        {
            if (e.State != EntityState.Added)
                continue;

            MailboxMetrics.RecordChangeEvent("item", e.Entity.EventType);
            db.PendingChangeNotifications.Add(new MailboxChangedEvent(
                e.Entity.UserId, e.Entity.CollectionId, e.Entity.ServerId, ToKind(e.Entity.EventType)));
        }

        foreach (var e in db.ChangeTracker.Entries<DbFolderEvent>())
        {
            if (e.State != EntityState.Added)
                continue;

            MailboxMetrics.RecordChangeEvent("folder", e.Entity.EventType);
            db.PendingChangeNotifications.Add(new MailboxChangedEvent(
                e.Entity.UserId, FolderHierarchyCollectionId, e.Entity.ServerId, ToKind(e.Entity.EventType)));
        }
    }

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

}
