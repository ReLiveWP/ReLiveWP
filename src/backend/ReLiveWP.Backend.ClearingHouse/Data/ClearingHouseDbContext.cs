using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ClearingHouse.Services.Mirror;

namespace ReLiveWP.Backend.ClearingHouse.Data;

public class DbSyncSource
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string ConnectionId { get; set; } = null!;
    public MirrorKind Kind { get; set; }
    public string ServiceId { get; set; } = null!;
    public string SourceId { get; set; } = null!;
    public bool SyncEnabled { get; set; }

    public string? DeltaToken { get; set; }

    // calendar mirrors one remote collection onto one folder; contacts merge into the default one
    public string? FolderId { get; set; }

    // what the remote calls it, versus what we last pushed to the folder. comparing the folder's
    // live name instead cannot tell a remote rename from one the user made on the phone.
    public string? RemoteDisplayName { get; set; }
    public string? FolderDisplayName { get; set; }

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
    public DbSet<DbSyncSource> SyncSources { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbSyncSource>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Kind).HasConversion<string>();
            e.HasIndex(x => new { x.UserId, x.ConnectionId, x.Kind, x.SourceId }).IsUnique();
            e.HasIndex(x => x.SyncEnabled);
        });
    }
}
