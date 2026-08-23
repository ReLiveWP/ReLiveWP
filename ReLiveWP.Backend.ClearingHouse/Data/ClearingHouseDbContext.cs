using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.ClearingHouse.Data;

public class DbContactSyncSource
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string ConnectionId { get; set; } = null!;
    public string ServiceId { get; set; } = null!;
    public string SourceId { get; set; } = null!;
    public bool SyncEnabled { get; set; }
    
    public string? DeltaToken { get; set; }

    public DateTime? LastSyncedAt { get; set; }
    public DateTime? LastAttemptedAt { get; set; }
    public DateTime? LastFailureAt { get; set; }
    public string? LastFailure { get; set; }
    public int ConsecutiveFailures { get; set; }

    public DateTime? RunRequestedAt { get; set; }
    public DateTime? RunStartedAt { get; set; }

    public int LastRunCreated { get; set; }
    public int LastRunUpdated { get; set; }
    public int LastRunDeleted { get; set; }
    public int LastRunSkipped { get; set; }
}

public class ClearingHouseDbContext(DbContextOptions<ClearingHouseDbContext> options) : DbContext(options)
{
    public DbSet<DbContactSyncSource> ContactSyncSources { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbContactSyncSource>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.ConnectionId, x.SourceId }).IsUnique();
            e.HasIndex(x => x.SyncEnabled);
        });
    }
}
