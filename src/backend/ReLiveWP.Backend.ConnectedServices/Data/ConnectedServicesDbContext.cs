using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ReLiveWP.Backend.ConnectedServices.Services;

namespace ReLiveWP.Backend.ConnectedServices.Data;

public class ConnectedServicesDbContext(DbContextOptions<ConnectedServicesDbContext> options,
                                        ConnectionSecretProtector tokenProtector) : DbContext(options)
{
    public DbSet<LiveDPoPKey> DPoPKeys { get; set; }
    public DbSet<LiveConnectedService> ConnectedServices { get; set; }
    public DbSet<LiveOAuthClient> OAuthClients { get; set; }

    public ConnectionSecretProtector TokenProtector { get; } = tokenProtector;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, ConnectedServicesModelCacheKeyFactory>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<LiveConnectedService>()
            .HasOne(u => u.DPoPKey);

        var tokenConverter = new ConnectionTokenConverter(TokenProtector);
        builder.Entity<LiveConnectedService>(service =>
        {
            service.Property(s => s.AccessToken).HasConversion(tokenConverter);
            service.Property(s => s.RefreshToken).HasConversion(tokenConverter);
        });

        if (Database.IsNpgsql())
        {
            builder.Entity<LiveConnectedService>()
                .Property(s => s.RowVersion)
                .IsRowVersion();
        }
    }

    public int EncryptPlaintextTokens()
    {
        var protectedPattern = ConnectionTokenConverter.ProtectedPrefix + "%";
        var plaintextIds = Database.SqlQuery<Guid>($"""
            SELECT "Id" AS "Value" FROM "ConnectedServices"
            WHERE ("AccessToken" <> '' AND "AccessToken" NOT LIKE {protectedPattern})
               OR ("RefreshToken" <> '' AND "RefreshToken" NOT LIKE {protectedPattern})
            """).ToList();

        if (plaintextIds.Count == 0)
            return 0;

        var plaintextServices = ConnectedServices.Where(s => plaintextIds.Contains(s.Id)).ToList();
        foreach (var service in plaintextServices)
        {
            var entry = Entry(service);
            entry.Property(s => s.AccessToken).IsModified = true;
            entry.Property(s => s.RefreshToken).IsModified = true;
        }

        SaveChanges();

        return plaintextServices.Count;
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
