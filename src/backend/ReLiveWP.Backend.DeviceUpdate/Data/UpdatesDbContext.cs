using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Data;

public class UpdatesDbContext : DbContext
{
    protected UpdatesDbContext()
    {
    }

    public UpdatesDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Update> Updates { get; set; }
    public DbSet<UpdateMetadata> Metadata { get; set; }
    public DbSet<UpdateExtendedMetadata> ExtendedMetadata { get; set; }
    public DbSet<UpdatePrerequisite> Prerequisites { get; set; }
    public DbSet<UpdateBundle> Bundles { get; set; }
    public DbSet<UpdateFile> Files { get; set; }
    public DbSet<UpdateLocalization> Localizations { get; set; }
    public DbSet<UpdateFragment> Fragments { get; set; }
    public DbSet<AuthoredRevision> AuthoredRevisions { get; set; }
    public DbSet<UpdateDeployment> Deployments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseNpgsql("Host=localhost;Port=15432;Database=relive_deviceupdate;Username=relive;Password=relive");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var update = modelBuilder.Entity<Update>();
        update.HasKey(u => u.RevisionId);
        update.Property(u => u.RevisionId).ValueGeneratedNever();
        update.HasIndex(u => u.UpdateId);

        modelBuilder.Entity<UpdateMetadata>()
            .HasKey(m => m.RevisionId);

        modelBuilder.Entity<UpdateMetadata>()
            .HasOne(m => m.Update)
            .WithOne(u => u.Metadata)
            .HasForeignKey<UpdateMetadata>(m => m.RevisionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UpdateExtendedMetadata>()
            .HasKey(m => m.RevisionId);

        modelBuilder.Entity<UpdateExtendedMetadata>()
            .HasOne(m => m.Update)
            .WithOne(u => u.ExtendedMetadata)
            .HasForeignKey<UpdateExtendedMetadata>(m => m.RevisionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UpdatePrerequisite>()
            .HasKey(f => new { f.UpdateRevisionId, f.PrerequisiteUpdateId });

        modelBuilder.Entity<UpdatePrerequisite>()
            .HasIndex(p => p.PrerequisiteUpdateId);

        modelBuilder.Entity<UpdatePrerequisite>()
            .HasOne(p => p.Update)
            .WithMany(u => u.Prerequisites)
            .HasForeignKey(p => p.UpdateRevisionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UpdateBundle>()
            .HasKey(b => new { b.BundleRevisionId, b.BundledUpdateId });

        modelBuilder.Entity<UpdateBundle>()
            .HasIndex(b => b.BundledUpdateId);

        modelBuilder.Entity<UpdateBundle>()
            .HasOne(b => b.Bundle)
            .WithMany(u => u.BundledUpdates)
            .HasForeignKey(b => b.BundleRevisionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UpdateFile>()
            .HasKey(f => new { f.UpdateRevisionId, f.FileName });

        modelBuilder.Entity<UpdateFile>()
            .HasOne(f => f.Update)
            .WithMany(u => u.Files)
            .HasForeignKey(f => f.UpdateRevisionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UpdateLocalization>()
            .HasKey(l => new { l.UpdateRevisionId, l.Language });

        modelBuilder.Entity<UpdateLocalization>()
            .HasOne(l => l.Update)
            .WithMany(u => u.Localizations)
            .HasForeignKey(l => l.UpdateRevisionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UpdateFragment>()
            .HasKey(f => new { f.UpdateRevisionId, f.Ordinal });

        modelBuilder.Entity<UpdateFragment>()
            .HasOne(f => f.Update)
            .WithMany(u => u.Fragments)
            .HasForeignKey(f => f.UpdateRevisionId)
            .OnDelete(DeleteBehavior.Cascade);

        var authored = modelBuilder.Entity<AuthoredRevision>();
        authored.HasKey(r => new { r.UpdateId, r.RevisionNumber });
        authored.HasIndex(r => r.RevisionId).IsUnique();

        modelBuilder.Entity<UpdateDeployment>()
            .HasKey(d => new { d.Ring, d.UpdateId });
    }
}
