using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data.Entities;

namespace ReLiveWP.Backend.Mailbox.Data;

public class MailboxDbContext : DbContext
{
    protected MailboxDbContext() { }

    public MailboxDbContext(DbContextOptions options) : base(options) { }

    public DbSet<DbDeviceInfo> DeviceInfos { get; set; }
    public DbSet<DbFolder> Folders { get; set; }
    public DbSet<DbFolderEvent> FolderEvents { get; set; }
    public DbSet<DbSyncState> SyncStates { get; set; }
    public DbSet<DbItem> Items { get; set; }
    public DbSet<DbContactAnnotation> ContactAnnotations { get; set; }
    public DbSet<DbItemEvent> ItemEvents { get; set; }
    public DbSet<DbContactCategory> ContactCategories { get; set; }
    public DbSet<DbContactChild> ContactChildren { get; set; }
    public DbSet<DbCalendarAttendee> CalendarAttendees { get; set; }
    public DbSet<DbCalendarCategory> CalendarCategories { get; set; }
    public DbSet<DbCalendarException> CalendarExceptions { get; set; }
    public DbSet<DbCalendarExceptionAttendee> CalendarExceptionAttendees { get; set; }
    public DbSet<DbCalendarExceptionCategory> CalendarExceptionCategories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
            optionsBuilder
                .UseSqlite("Data Source=mailbox.db")
                .AddInterceptors(new ChangeLogInterceptor());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DbDeviceInfo>(e =>
        {
            e.HasIndex(d => new { d.UserId, d.DeviceId }).IsUnique();
        });

        modelBuilder.Entity<DbFolder>(e =>
        {
            e.HasIndex(f => new { f.UserId, f.Id }).IsUnique();
            e.HasIndex(f => f.UserId);
            e.HasMany(f => f.FolderItems)
             .WithOne(i => i.Collection)
             .HasForeignKey(i => i.CollectionId);
        });

        modelBuilder.Entity<DbFolderEvent>(e =>
        {
            e.HasIndex(ev => new { ev.UserId, ev.Id });
        });

        modelBuilder.Entity<DbSyncState>(e =>
        {
            e.HasIndex(d => new { d.UserId, d.DeviceId, d.CollectionId }).IsUnique();
        });

        modelBuilder.Entity<DbItem>(e =>
        {
            e.HasIndex(i => new { i.UserId, i.CollectionId, i.ServerId }).IsUnique();
            e.HasIndex(i => new { i.UserId, i.CollectionId });
            e.HasDiscriminator<string>("ItemClass")
                .HasValue<DbContactItem>("Contact")
                .HasValue<DbCalendarItem>("Calendar")
                .HasValue<DbTask>("Task")
                .HasValue<DbEmail>("Email");
        });

        // Subject and NativeBodyType would otherwise collide with DbCalendarItem's columns on
        // the shared TPH table. Give Email distinct column names so the existing calendar
        // columns are left untouched (otherwise EF reassigns the unprefixed column to Email).
        modelBuilder.Entity<DbEmail>(e =>
        {
            e.Property(m => m.Subject).HasColumnName("Email_Subject");
            e.Property(m => m.NativeBodyType).HasColumnName("Email_NativeBodyType");
        });

        modelBuilder.Entity<DbContactAnnotation>(e =>
        {
            e.HasKey(a => a.ContactItemId);
            e.HasOne(a => a.ContactItem)
             .WithOne(c => c.Annotation)
             .HasForeignKey<DbContactAnnotation>(a => a.ContactItemId);
        });

        modelBuilder.Entity<DbItemEvent>(e =>
        {
            e.HasIndex(ev => new { ev.UserId, ev.CollectionId, ev.Id });
        });

        modelBuilder.Entity<DbContactCategory>(e =>
        {
            e.HasOne(c => c.ContactItem)
                .WithMany(i => i.Categories)
                .HasForeignKey(c => c.ContactItemId);
        });

        modelBuilder.Entity<DbContactChild>(e =>
        {
            e.HasOne(c => c.ContactItem)
                .WithMany(i => i.Children)
                .HasForeignKey(c => c.ContactItemId);
        });

        modelBuilder.Entity<DbCalendarAttendee>(e =>
        {
            e.HasOne(a => a.CalendarItem)
                .WithMany(i => i.Attendees)
                .HasForeignKey(a => a.CalendarItemId);
        });

        modelBuilder.Entity<DbCalendarCategory>(e =>
        {
            e.HasOne(c => c.CalendarItem)
                .WithMany(i => i.Categories)
                .HasForeignKey(c => c.CalendarItemId);
        });

        modelBuilder.Entity<DbCalendarException>(e =>
        {
            e.HasOne(ex => ex.CalendarItem)
                .WithMany(i => i.Exceptions)
                .HasForeignKey(ex => ex.CalendarItemId);
        });

        modelBuilder.Entity<DbCalendarExceptionAttendee>(e =>
        {
            e.HasOne(a => a.CalendarException)
                .WithMany(ex => ex.Attendees)
                .HasForeignKey(a => a.CalendarExceptionId);
        });

        modelBuilder.Entity<DbCalendarExceptionCategory>(e =>
        {
            e.HasOne(c => c.CalendarException)
                .WithMany(ex => ex.Categories)
                .HasForeignKey(c => c.CalendarExceptionId);
        });
    }
}
