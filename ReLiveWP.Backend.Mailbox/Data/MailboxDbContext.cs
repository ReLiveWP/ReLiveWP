using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.ServiceDefaults.Events;

namespace ReLiveWP.Backend.Mailbox.Data;

public class MailboxDbContext : DbContext
{
    protected MailboxDbContext() { }

    public MailboxDbContext(DbContextOptions options) : base(options) { }

    // not a table; ChangeLogInterceptor buffers events here for redis pubsub, never persisted
    [NotMapped]
    internal List<MailboxChangedEvent> PendingChangeNotifications { get; } = [];

    public DbSet<DbDeviceInfo> DeviceInfos { get; set; }

    public DbSet<DbFolder> Folders { get; set; }
    public DbSet<DbFolderEvent> FolderEvents { get; set; }

    public DbSet<DbSyncState> SyncStates { get; set; }

    public DbSet<DbItem> Items { get; set; }
    public DbSet<DbItemEvent> ItemEvents { get; set; }

    public DbSet<DbContactAnnotation> ContactAnnotations { get; set; }
    public DbSet<DbContactEmail> ContactEmails { get; set; }
    public DbSet<DbContactIdentity> ContactIdentities { get; set; }
    public DbSet<DbContactCategory> ContactCategories { get; set; }
    public DbSet<DbContactChild> ContactChildren { get; set; }

    public DbSet<DbNoteCategory> NoteCategories { get; set; }

    public DbSet<DbAttachment> Attachments { get; set; }

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
                .UseNpgsql("Host=localhost;Database=relive_mailbox;Username=relive;Password=relive")
                .AddInterceptors(new ItemValidationInterceptor(), new ChangeLogInterceptor());
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
            // reads cursor on CommitId and tie-break on Id, so the index has to lead with it
            e.HasIndex(ev => new { ev.UserId, ev.CommitId, ev.Id });
            if (Database.IsNpgsql())
                e.Property(ev => ev.CommitId).HasDefaultValueSql(ChangeEventCursor.CommitIdDefaultSql);
        });

        modelBuilder.Entity<DbSyncState>(e =>
        {
            e.HasIndex(d => new { d.UserId, d.DeviceId, d.CollectionId }).IsUnique();
            if (Database.IsNpgsql())
            {
                e.Property<uint>("xmin")
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
            }
        });

        modelBuilder.Entity<DbItem>(e =>
        {
            e.HasIndex(i => new { i.UserId, i.CollectionId, i.ServerId }).IsUnique();
            e.HasIndex(i => new { i.UserId, i.CollectionId });
            // supports GetItem/UpdateItem/DeleteItem/MoveItem/GetItems lookups by (UserId, ServerId)
            // alone; the composite unique index above leads with CollectionId so can't serve these
            e.HasIndex(i => new { i.UserId, i.ServerId });
            e.HasIndex(i => new { i.UserId, i.CollectionId, i.ClientId })
                .IsUnique()
                .HasFilter("\"ClientId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            e.HasIndex(i => new { i.UserId, i.OriginServiceId, i.OriginCollectionId, i.OriginExternalId })
                .IsUnique()
                .HasFilter("\"OriginServiceId\" IS NOT NULL AND \"DeletedAt\" IS NULL");
                
            if (Database.IsNpgsql())
            {
                e.Property(i => i.Version)
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
            }
            else
            {
                // no xmin outside Postgres; don't invent a real column for it
                e.Ignore(i => i.Version);
            }

            e.HasDiscriminator<string>("ItemClass")
                .HasValue<DbContactItem>("Contact")
                .HasValue<DbCalendarItem>("Calendar")
                .HasValue<DbTask>("Task")
                .HasValue<DbEmail>("Email")
                .HasValue<DbNote>("Note");
        });

        // Subject/NativeBodyType would otherwise collide with DbCalendarItem's columns on the shared TPH table
        modelBuilder.Entity<DbEmail>(e =>
        {
            e.Property(m => m.Subject).HasColumnName("Email_Subject");
            e.Property(m => m.NativeBodyType).HasColumnName("Email_NativeBodyType");
            // supports MoveConversation's per-user conversation scan
            e.HasIndex(m => new { m.UserId, m.ConversationId });
        });

        // DbTask shares the TPH table with Calendar; prefix every column so none collide
        modelBuilder.Entity<DbTask>(e =>
        {
            foreach (var p in e.Metadata.GetDeclaredProperties())
                e.Property(p.Name).HasColumnName($"Task_{p.Name}");
        });

        // same TPH collision as DbTask above
        modelBuilder.Entity<DbNote>(e =>
        {
            foreach (var p in e.Metadata.GetDeclaredProperties())
                e.Property(p.Name).HasColumnName($"Note_{p.Name}");
        });

        modelBuilder.Entity<DbContactAnnotation>(e =>
        {
            e.HasKey(a => a.ContactItemId);
            e.HasOne(a => a.ContactItem)
             .WithOne(c => c.Annotation)
             .HasForeignKey<DbContactAnnotation>(a => a.ContactItemId);
            // supports GetContactProfiles' cid lookup join
            e.HasIndex(a => a.Cid);
        });

        modelBuilder.Entity<DbContactEmail>(e =>
        {
            e.HasKey(x => x.Id);
            // the bare address index serves the fan-out when a user's visibility changes,
            // the owner-scoped one answers the mutual reciprocity check
            e.HasIndex(x => x.NormalizedAddress);
            e.HasIndex(x => new { x.UserId, x.NormalizedAddress });
            e.HasOne(x => x.ContactItem)
             .WithMany()
             .HasForeignKey(x => x.ContactItemId);
        });

        modelBuilder.Entity<DbContactIdentity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.Provider, x.ExternalId }).IsUnique();
            e.HasIndex(x => new { x.UserId, x.ContactCid });
            e.HasOne(x => x.ContactItem)
             .WithMany()
             .HasForeignKey(x => x.ContactItemId);
        });

        modelBuilder.Entity<DbItemEvent>(e =>
        {
            e.HasIndex(ev => new { ev.UserId, ev.CollectionId, ev.Id });
            e.HasIndex(ev => new { ev.UserId, ev.CollectionId, ev.CommitId, ev.Id });
            if (Database.IsNpgsql())
                e.Property(ev => ev.CommitId).HasDefaultValueSql(ChangeEventCursor.CommitIdDefaultSql);
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

        modelBuilder.Entity<DbNoteCategory>(e =>
        {
            e.HasOne(c => c.NoteItem)
                .WithMany(i => i.Categories)
                .HasForeignKey(c => c.NoteItemId);
        });

        modelBuilder.Entity<DbAttachment>(e =>
        {
            e.HasOne(a => a.EmailItem)
                .WithMany(i => i.Attachments)
                .HasForeignKey(a => a.EmailItemId);
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
