using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.SkyDrive.Data;

public class SkyDriveDbContext : DbContext
{
    protected SkyDriveDbContext()
    {
    }

    public SkyDriveDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<SkyLibrary> Libraries { get; set; }
    public DbSet<SkyAlbumItem> AlbumItems { get; set; }
    public DbSet<SkyProviderAlbum> ProviderAlbums { get; set; }
    public DbSet<SkyAlbumCover> AlbumCovers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseNpgsql("Host=localhost;Database=relive_skydrive;Username=relive;Password=relive");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SkyLibrary>()
            .HasIndex(l => new { l.OwnerId, l.Category })
            .IsUnique();

        modelBuilder.Entity<SkyAlbumItem>(b =>
        {
            b.HasKey(i => new { i.OwnerId, i.Provider, i.ProviderItemId });
            b.HasIndex(i => new { i.OwnerId, i.Album });
        });

        modelBuilder.Entity<SkyProviderAlbum>()
            .HasKey(a => new { a.OwnerId, a.Album, a.Provider });

        modelBuilder.Entity<SkyAlbumCover>()
            .HasKey(c => new { c.OwnerId, c.AlbumRef });
    }
}
