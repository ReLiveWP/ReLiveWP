using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.Identity.Data;

public class LiveDbContext(DbContextOptions<LiveDbContext> options) 
    : IdentityDbContext<LiveUser, LiveRole, Guid>(options)
{
    public DbSet<LivePendingOAuth> PendingOAuths { get; set; }
    public DbSet<LiveConnectedService> ConnectedServices { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<LiveUser>()
            .HasMany(p => p.PendingOAuths)
            .WithOne(p => p.User);

        builder.Entity<LiveUser>()
            .HasMany(p => p.ConnectedServices)
            .WithOne(p => p.User);
    }
}