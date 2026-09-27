using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Catalog;

// Hands out revision ids for authored updates. Not safe to run twice at once, same as the crawler:
// compiling the catalog is a manual single-operator job.
public class RevisionIdAllocator(UpdatesDbContext db)
{
    // Upstream ids in the catalog top out around 37.5 million, but the live Microsoft catalog issues
    // far higher ones and a wider crawl would pull them in, so the band sits well clear rather than
    // just above what we happen to hold.
    public const long Floor = 900_000_000;

    // Deployment/ID goes on the wire as s:int after RevisionId + 0x40000000, so this is the point
    // where that addition overflows.
    public const long Ceiling = int.MaxValue - 0x40000000;

    // Rows added earlier in the same compile are only in the change tracker, so the ledger query
    // cannot see them and every update in one run would otherwise be handed the same id.
    private long lastAllocated = Floor - 1;

    public async Task<long> AllocateAsync(Guid updateId, int revisionNumber, string contentHash)
    {
        var existing = await db.AuthoredRevisions
            .FirstOrDefaultAsync(r => r.UpdateId == updateId && r.RevisionNumber == revisionNumber);

        if (existing is not null)
        {
            if (existing.ContentHash != contentHash)
                throw new InvalidOperationException(
                    $"Update {updateId} revision {revisionNumber} has already been compiled with different content. " +
                    "Bump RevisionNumber; a shipped revision id can never be reused.");

            return existing.RevisionId;
        }

        var highest = await db.AuthoredRevisions
            .Where(r => r.RevisionNumber < revisionNumber && r.UpdateId == updateId)
            .OrderByDescending(r => r.RevisionNumber)
            .FirstOrDefaultAsync();

        if (highest is null && await db.AuthoredRevisions.AnyAsync(r => r.UpdateId == updateId))
            throw new InvalidOperationException(
                $"Update {updateId} already has a revision newer than {revisionNumber}. " +
                "Revisions only move forward.");

        var allocated = Math.Max(Floor, Math.Max(await NextFreeAsync(), lastAllocated + 1));
        if (allocated > Ceiling)
            throw new InvalidOperationException(
                $"Revision id {allocated} is past {Ceiling}, where Deployment/ID overflows s:int.");

        db.AuthoredRevisions.Add(new AuthoredRevision
        {
            UpdateId = updateId,
            RevisionNumber = revisionNumber,
            RevisionId = allocated,
            ContentHash = contentHash,
        });

        lastAllocated = allocated;
        return allocated;
    }

    private async Task<long> NextFreeAsync()
    {
        var allocated = await db.AuthoredRevisions
            .Where(r => r.RevisionId >= Floor)
            .Select(r => (long?)r.RevisionId)
            .MaxAsync();

        // The ledger is the record, but a revision could have been written without one if the
        // catalog was restored from elsewhere, so the catalog itself gets a say too.
        var served = await db.Updates
            .Where(u => u.RevisionId >= Floor)
            .Select(u => (long?)u.RevisionId)
            .MaxAsync();

        return Math.Max(allocated ?? Floor - 1, served ?? Floor - 1) + 1;
    }

    public static string HashContent(string coreXml, string extendedXml) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(coreXml + extendedXml)));
}
