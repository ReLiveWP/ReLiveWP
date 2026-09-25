using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ReLiveWP.Backend.Mailbox.Data.Entities;

namespace ReLiveWP.Backend.Mailbox.Data;

// resolves a tracked child row back to the tracked item it hangs off, built once per save
internal sealed class TrackedItemIndex
{
    private readonly Dictionary<string, DbItem> _items = [];
    private readonly Dictionary<string, DbCalendarException> _exceptions = [];

    public TrackedItemIndex(MailboxDbContext db)
    {
        foreach (var entry in db.ChangeTracker.Entries())
        {
            switch (entry.Entity)
            {
                case DbItem item:
                    _items[item.Id] = item;
                    break;
                case DbCalendarException exception:
                    _exceptions[exception.Id] = exception;
                    break;
            }
        }
    }

    public static bool IsChildMutation(EntityEntry entry) =>
        entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;

    public DbItem? ParentItemOf(object entity) => entity switch
    {
        DbContactCategory c => ItemOrNull(c.ContactItemId),
        DbContactChild c => ItemOrNull(c.ContactItemId),
        DbContactAnnotation a => ItemOrNull(a.ContactItemId),
        DbCalendarAttendee a => ItemOrNull(a.CalendarItemId),
        DbCalendarCategory c => ItemOrNull(c.CalendarItemId),
        DbCalendarException e => ItemOrNull(e.CalendarItemId),
        DbCalendarExceptionAttendee a => ThroughException(a.CalendarExceptionId),
        DbCalendarExceptionCategory c => ThroughException(c.CalendarExceptionId),
        DbNoteCategory c => ItemOrNull(c.NoteItemId),
        _ => null,
    };

    private DbItem? ItemOrNull(string itemId) =>
        _items.TryGetValue(itemId, out var item) ? item : null;

    private DbItem? ThroughException(string exceptionId) =>
        _exceptions.TryGetValue(exceptionId, out var exception) ? ItemOrNull(exception.CalendarItemId) : null;
}
