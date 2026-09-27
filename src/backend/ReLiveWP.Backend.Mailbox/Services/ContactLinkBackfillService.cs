using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Mailbox.Services;

// contacts that predate the address index have no rows to match on, and nobody's links have been
// worked out yet. Both passes are safe to run again on every start.
public class ContactLinkBackfillService(
    IServiceScopeFactory scopeFactory,
    ILogger<ContactLinkBackfillService> logger) : BackgroundService
{
    private const int BatchSize = 200;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MailboxDbContext>();

            var seeded = await SeedAddressesAsync(db, stoppingToken);
            if (seeded > 0)
                logger.LogInformation("indexed {Count} address(es) on existing contacts", seeded);

            var resolver = scope.ServiceProvider.GetRequiredService<ContactLinkResolver>();
            var users = scope.ServiceProvider.GetRequiredService<User.UserClient>();

            using var call = users.ListUsers(new ListUsersRequest(), cancellationToken: stoppingToken);
            await foreach (var user in call.ResponseStream.ReadAllAsync(stoppingToken))
                await resolver.ReconcileFromIdentityAsync(user.Id, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "contact link backfill skipped; will retry next start");
        }
    }

    private static async Task<int> SeedAddressesAsync(MailboxDbContext db, CancellationToken ct)
    {
        var total = 0;
        var after = "";

        while (!ct.IsCancellationRequested)
        {
            // paged on the id rather than the predicate: a me contact or a whitespace-only address
            // adds no rows, so it would keep matching and the batch would never move
            var batch = await db.Items.OfType<DbContactItem>()
                .Include(c => c.Annotation)
                .Where(c => c.DeletedAt == null
                            && string.Compare(c.Id, after) > 0
                            && (c.Email1Address != null || c.Email2Address != null || c.Email3Address != null)
                            && !db.ContactEmails.Any(e => e.ContactItemId == c.Id))
                .OrderBy(c => c.Id)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (batch.Count == 0)
                break;

            foreach (var contact in batch)
            {
                if (ContactAddresses.IsMeContact(contact))
                    continue;

                foreach (var address in ContactAddresses.Of(contact))
                {
                    db.ContactEmails.Add(new DbContactEmail
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        UserId = contact.UserId,
                        ContactItemId = contact.Id,
                        NormalizedAddress = address,
                    });
                    total++;
                }
            }

            await db.SaveChangesAsync(ct);
            after = batch[^1].Id;
        }

        return total;
    }
}
