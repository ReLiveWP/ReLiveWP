using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.ConnectedServices.Data;

public class ConnectedServicesDbContext(DbContextOptions<ConnectedServicesDbContext> options) : DbContext(options)
{
    public DbSet<LiveDPoPKey> DPoPKeys { get; set; }
    public DbSet<LiveConnectedService> ConnectedServices { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<LiveConnectedService>()
            .HasOne(u => u.DPoPKey);

        if (Database.IsNpgsql())
        {
            builder.Entity<LiveConnectedService>()
                .Property(s => s.RowVersion)
                .IsRowVersion();
        }
    }

    public async Task SaveChangesOverConcurrentWritesAsync(CancellationToken ct = default)
    {
        try
        {
            await SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
            {
                // a replaced owned profile is Added, so every value on it is ours
                if (entry.State == EntityState.Added)
                    continue;

                var databaseValues = await entry.GetDatabaseValuesAsync(ct);
                if (databaseValues == null)
                {
                    entry.State = EntityState.Detached;
                    continue;
                }

                var untouched = entry.Properties.Where(p => !p.IsModified).ToList();

                entry.OriginalValues.SetValues(databaseValues);
                foreach (var property in untouched)
                    property.CurrentValue = databaseValues[property.Metadata];
            }

            await SaveChangesAsync(ct);
        }
    }
}
