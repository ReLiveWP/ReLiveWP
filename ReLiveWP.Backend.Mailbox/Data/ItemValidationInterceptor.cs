using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Validation;

namespace ReLiveWP.Backend.Mailbox.Data;

// runs the validation/repair suite on items before they hit the database
public sealed class ItemValidationInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        var db = (MailboxDbContext)eventData.Context!;
        var items = CollectItems(db);
        if (items.Count > 0)
            Apply(items, ResolveFolderTypes(db, items));
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        var db = (MailboxDbContext)eventData.Context!;
        var items = CollectItems(db);
        if (items.Count > 0)
            Apply(items, await ResolveFolderTypesAsync(db, items, ct));
        return await base.SavingChangesAsync(eventData, result, ct);
    }

    private static List<DbItem> CollectItems(MailboxDbContext db) =>
        [.. db.ChangeTracker.Entries<DbItem>()
              .Where(e => e.State is EntityState.Added or EntityState.Modified)
              .Select(e => e.Entity)
              .Where(i => i.ValidationFlaggedAt is null)];

    private static Dictionary<string, DbFolderType> ResolveFolderTypes(MailboxDbContext db, List<DbItem> items)
    {
        var (map, missing) = ResolveLocal(db, items);
        if (missing.Count > 0)
            foreach (var row in db.Folders.Where(f => missing.Contains(f.Id))
                                          .Select(f => new { f.Id, f.Type }).ToList())
                map[row.Id] = row.Type;
        return map;
    }

    private static async Task<Dictionary<string, DbFolderType>> ResolveFolderTypesAsync(
        MailboxDbContext db, List<DbItem> items, CancellationToken ct)
    {
        var (map, missing) = ResolveLocal(db, items);
        if (missing.Count > 0)
            foreach (var row in await db.Folders.Where(f => missing.Contains(f.Id))
                                                .Select(f => new { f.Id, f.Type }).ToListAsync(ct))
                map[row.Id] = row.Type;
        return map;
    }

    private static (Dictionary<string, DbFolderType> map, List<string> missing) ResolveLocal(
        MailboxDbContext db, List<DbItem> items)
    {
        var map = new Dictionary<string, DbFolderType>();
        var missing = new List<string>();
        foreach (var id in items.Select(i => i.CollectionId).Distinct())
        {
            var local = db.Folders.Local.FirstOrDefault(f => f.Id == id);
            if (local is not null) map[id] = local.Type;
            else missing.Add(id);
        }
        return (map, missing);
    }

    private static void Apply(List<DbItem> items, Dictionary<string, DbFolderType> folderTypes)
    {
        var rejected = new List<ValidationIssue>();

        foreach (var item in items)
        {
            DbFolderType? ft = folderTypes.TryGetValue(item.CollectionId, out var t) ? t : null;
            rejected.AddRange(ItemValidationRules.ValidateAndCorrect(item, ft)
                .Where(i => i.Severity == ValidationSeverity.Rejected));
        }

        if (rejected.Count > 0)
            throw new ItemValidationException(rejected);
    }
}
