using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.Identity.Data;

public class LiveDbContext(DbContextOptions<LiveDbContext> options)
    : IdentityDbContext<LiveUser, LiveRole, Guid>(options)
{
    public DbSet<LiveRefreshToken> LiveRefreshTokens => Set<LiveRefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<LiveUser>()
            .OwnsMany(p => p.Certificates, c =>
            {
                c.WithOwner()
                 .HasForeignKey(o => o.UserId);
                c.HasKey(o => o.Fingerprint);
            });
    }
}
