using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Validation;

namespace ReLiveWP.Backend.Mailbox.Services;

public class MailboxIntegrityService(MailboxDbContext db)
{
    private const int PageSize = 500;

    public async Task<SweepResult> SweepAsync(
        string? userId, string? collectionId, CancellationToken ct)
    {
        // repairs invalid items where possible, otherwise flags them so they're withheld from devices
        int scanned = 0, corrected = 0, flagged = 0;
        var folderTypes = new Dictionary<string, DbFolderType>();
        int offset = 0;

        while (true)
        {
            var query = db.Items.Where(i => i.DeletedAt == null);
            if (!string.IsNullOrEmpty(userId)) query = query.Where(i => i.UserId == userId);
            if (!string.IsNullOrEmpty(collectionId)) query = query.Where(i => i.CollectionId == collectionId);

            var page = await query.OrderBy(i => i.Id)
                .Skip(offset)
                .Take(PageSize)
                .ToListAsync(ct);

            if (page.Count == 0) 
                break;
            offset += page.Count;

            await LoadCalendarExceptionsAsync(page, ct);
            await ResolveFolderTypesAsync(page, folderTypes, ct);

            foreach (var item in page)
            {
                scanned++;
                DbFolderType? ft = folderTypes.TryGetValue(item.CollectionId, out var t) ? t : null;
                var issues = ItemValidationRules.ValidateAndCorrect(item, ft);
                var rejects = issues.Where(i => i.Severity == ValidationSeverity.Rejected).ToList();

                if (rejects.Count > 0)
                {
                    item.ValidationFlaggedAt ??= DateTime.UtcNow;
                    item.ValidationReason = string.Join("; ",
                        rejects.Select(r => $"{r.Rule} ({r.Field}): {r.Message}"));
                    flagged++;
                }
                else
                {
                    if (item.ValidationFlaggedAt is not null)
                    {
                        item.ValidationFlaggedAt = null;
                        item.ValidationReason = null;
                    }

                    if (issues.Any(i => i.Severity == ValidationSeverity.Corrected))
                        corrected++;
                }
            }

            await db.SaveChangesAsync(ct);
        }

        return new SweepResult(scanned, corrected, flagged);
    }

    private async Task LoadCalendarExceptionsAsync(List<DbItem> page, CancellationToken ct)
    {
        var calIds = page.OfType<DbCalendarItem>().Select(c => c.Id).ToList();
        if (calIds.Count == 0) return;

        var exceptions = await db.CalendarExceptions
            .Where(x => calIds.Contains(x.CalendarItemId)).ToListAsync(ct);
        var byCal = exceptions.ToLookup(x => x.CalendarItemId);
        foreach (var c in page.OfType<DbCalendarItem>())
            c.Exceptions = [.. byCal[c.Id]];
    }

    private async Task ResolveFolderTypesAsync(
        List<DbItem> page, Dictionary<string, DbFolderType> cache, CancellationToken ct)
    {
        var need = page.Select(i => i.CollectionId).Distinct().Where(id => !cache.ContainsKey(id)).ToList();
        if (need.Count == 0) return;

        foreach (var row in await db.Folders.Where(f => need.Contains(f.Id))
                                            .Select(f => new { f.Id, f.Type }).ToListAsync(ct))
            cache[row.Id] = row.Type;
    }
}

public record struct SweepResult(int Scanned, int Corrected, int Flagged)
{
    public static implicit operator (int scanned, int corrected, int flagged)(SweepResult value)
    {
        return (value.Scanned, value.Corrected, value.Flagged);
    }

    public static implicit operator SweepResult((int scanned, int corrected, int flagged) value)
    {
        return new SweepResult(value.scanned, value.corrected, value.flagged);
    }
}