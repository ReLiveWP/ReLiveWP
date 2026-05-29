using Microsoft.EntityFrameworkCore;
using ReLiveWP.Services.Exchange.Data.Entities;

namespace ReLiveWP.Services.Exchange.Data;

public class ExchangeDbContext : DbContext
{
    protected ExchangeDbContext() { }

    public ExchangeDbContext(DbContextOptions options) : base(options) { }

    public DbSet<DeviceInfo> DeviceInfos { get; set; }
    public DbSet<Folder> Folders { get; set; }
    public DbSet<FolderEvent> FolderEvents { get; set; }
    public DbSet<SyncState> SyncStates { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<ContactAnnotation> ContactAnnotations { get; set; }
    public DbSet<ItemEvent> ItemEvents { get; set; }
    public DbSet<ContactCategory> ContactCategories { get; set; }
    public DbSet<ContactChild> ContactChildren { get; set; }
    public DbSet<CalendarAttendee> CalendarAttendees { get; set; }
    public DbSet<CalendarCategory> CalendarCategories { get; set; }
    public DbSet<CalendarException> CalendarExceptions { get; set; }
    public DbSet<CalendarExceptionAttendee> CalendarExceptionAttendees { get; set; }
    public DbSet<CalendarExceptionCategory> CalendarExceptionCategories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSqlite("Data Source=exchange.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DeviceInfo>(e =>
        {
            e.HasIndex(d => new { d.UserId, d.DeviceId }).IsUnique();
        });

        modelBuilder.Entity<Folder>(e =>
        {
            e.HasIndex(f => new { f.UserId, f.Id }).IsUnique();
            e.HasIndex(f => f.UserId);

            e.HasMany(e => e.FolderItems)
             .WithOne(e => e.Collection)
             .HasForeignKey(e => e.CollectionId);
        });

        modelBuilder.Entity<FolderEvent>(e =>
        {
            e.HasIndex(ev => new { ev.UserId, ev.Id });
        });

        modelBuilder.Entity<SyncState>(e =>
        {
            e.HasIndex(d => new { d.UserId, d.DeviceId, d.CollectionId })
             .IsUnique();
        });

        modelBuilder.Entity<Item>(e =>
        {
            e.HasIndex(i => new { i.UserId, i.CollectionId, i.ServerId }).IsUnique();
            e.HasIndex(i => new { i.UserId, i.CollectionId });
            e.HasDiscriminator<string>("ItemClass")
                .HasValue<ContactItem>("Contact")
                .HasValue<CalendarItem>("Calendar")
                .HasValue<TaskItem>("Task")
                .HasValue<EmailItem>("Email");
        });

        modelBuilder.Entity<ContactAnnotation>(e =>
        {
            e.HasKey(a => a.ContactItemId);
            e.HasOne(a => a.ContactItem)
             .WithOne(c => c.Annotation)
             .HasForeignKey<ContactAnnotation>(a => a.ContactItemId);
        });

        modelBuilder.Entity<ItemEvent>(e =>
        {
            e.HasIndex(ev => new { ev.UserId, ev.CollectionId, ev.Id });
        });

        modelBuilder.Entity<ContactCategory>(e =>
        {
            e.HasOne(c => c.ContactItem)
                .WithMany(i => i.Categories)
                .HasForeignKey(c => c.ContactItemId);
        });

        modelBuilder.Entity<ContactChild>(e =>
        {
            e.HasOne(c => c.ContactItem)
                .WithMany(i => i.Children)
                .HasForeignKey(c => c.ContactItemId);
        });

        modelBuilder.Entity<CalendarAttendee>(e =>
        {
            e.HasOne(a => a.CalendarItem)
                .WithMany(i => i.Attendees)
                .HasForeignKey(a => a.CalendarItemId);
        });

        modelBuilder.Entity<CalendarCategory>(e =>
        {
            e.HasOne(c => c.CalendarItem)
                .WithMany(i => i.Categories)
                .HasForeignKey(c => c.CalendarItemId);
        });

        modelBuilder.Entity<CalendarException>(e =>
        {
            e.HasOne(ex => ex.CalendarItem)
                .WithMany(i => i.Exceptions)
                .HasForeignKey(ex => ex.CalendarItemId);
        });

        modelBuilder.Entity<CalendarExceptionAttendee>(e =>
        {
            e.HasOne(a => a.CalendarException)
                .WithMany(ex => ex.Attendees)
                .HasForeignKey(a => a.CalendarExceptionId);
        });

        modelBuilder.Entity<CalendarExceptionCategory>(e =>
        {
            e.HasOne(c => c.CalendarException)
                .WithMany(ex => ex.Categories)
                .HasForeignKey(c => c.CalendarExceptionId);
        });
    }
}
