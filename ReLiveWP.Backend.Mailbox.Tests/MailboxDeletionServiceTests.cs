using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Services;

namespace ReLiveWP.Backend.Mailbox.Tests;

// GDPR-style hard delete: everything for a UserId must go, cascading to children via the FK
// chain configured in MailboxDbContext, while another user's rows are left untouched.
public class MailboxDeletionServiceTests : IDisposable
{
    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";

    private readonly SqliteConnection _connection;

    public MailboxDeletionServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private MailboxDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<MailboxDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new MailboxDbContext(options);
    }

    [Fact]
    public async Task DeleteAsync_hard_deletes_everything_for_the_user_and_cascades_to_children()
    {
        const string folderId = "folder-1";
        const string contactId = "contact-1";
        const string emailId = "email-1";
        const string attachmentId = "att-1";

        await using (var db = NewContext())
        {
            db.Folders.Add(new DbFolder { Id = folderId, UserId = UserId, DisplayName = "Contacts", Type = DbFolderType.ContactsDefault });
            db.Items.Add(new DbContactItem { Id = contactId, ServerId = contactId, UserId = UserId, CollectionId = folderId, FirstName = "Ada" });
            db.ContactCategories.Add(new DbContactCategory { Id = "cat-1", ContactItemId = contactId, Name = "Friends" });
            db.ContactAnnotations.Add(new DbContactAnnotation { ContactItemId = contactId, Cid = 42 });
            db.ContactIdentities.Add(new DbContactIdentity
            { Id = "ident-1", UserId = UserId, ContactItemId = contactId, ContactCid = 42, Provider = "atproto", ExternalId = "did:1" });
            db.Items.Add(new DbEmail { Id = emailId, ServerId = emailId, UserId = UserId, CollectionId = folderId });
            db.Attachments.Add(new DbAttachment { Id = attachmentId, EmailItemId = emailId, DisplayName = "a.txt" });
            db.SyncStates.Add(new DbSyncState { UserId = UserId, DeviceId = "device-1", CollectionId = folderId, LastSeenAt = DateTime.UtcNow });
            db.DeviceInfos.Add(new DbDeviceInfo { UserId = UserId, DeviceId = "device-1", UpdatedAt = DateTime.UtcNow });
            db.ItemEvents.Add(new DbItemEvent
            { UserId = UserId, CollectionId = folderId, ServerId = emailId, EventType = DbChangeEventType.Add, OccurredAt = DateTime.UtcNow });
            db.FolderEvents.Add(new DbFolderEvent
            { UserId = UserId, ServerId = folderId, EventType = DbChangeEventType.Add, OccurredAt = DateTime.UtcNow });

            // a different user's rows must survive
            db.Folders.Add(new DbFolder { Id = "other-folder", UserId = OtherUserId, DisplayName = "Inbox", Type = DbFolderType.InboxDefault });
            db.SyncStates.Add(new DbSyncState { UserId = OtherUserId, DeviceId = "device-2", CollectionId = "other-folder", LastSeenAt = DateTime.UtcNow });

            await db.SaveChangesAsync();
        }

        await using (var db = NewContext())
        {
            var result = await new MailboxDeletionService(db).DeleteAsync(UserId);
            Assert.Equal(1, result.FoldersDeleted);
            Assert.Equal(2, result.ItemsDeleted);
        }

        await using var verify = NewContext();
        Assert.Empty(await verify.Folders.Where(f => f.UserId == UserId).ToListAsync());
        Assert.Empty(await verify.Items.Where(i => i.UserId == UserId).ToListAsync());
        Assert.Empty(await verify.ContactCategories.ToListAsync());
        Assert.Empty(await verify.ContactAnnotations.ToListAsync());
        Assert.Empty(await verify.ContactIdentities.Where(x => x.UserId == UserId).ToListAsync());
        Assert.Empty(await verify.Attachments.ToListAsync());
        Assert.Empty(await verify.SyncStates.Where(s => s.UserId == UserId).ToListAsync());
        Assert.Empty(await verify.DeviceInfos.Where(d => d.UserId == UserId).ToListAsync());
        Assert.Empty(await verify.ItemEvents.Where(e => e.UserId == UserId).ToListAsync());
        Assert.Empty(await verify.FolderEvents.Where(e => e.UserId == UserId).ToListAsync());

        Assert.Single(await verify.Folders.Where(f => f.UserId == OtherUserId).ToListAsync());
        Assert.Single(await verify.SyncStates.Where(s => s.UserId == OtherUserId).ToListAsync());
    }

    [Fact]
    public async Task DeleteAsync_on_a_user_with_no_data_is_a_no_op()
    {
        await using var db = NewContext();
        var result = await new MailboxDeletionService(db).DeleteAsync("no-such-user");
        Assert.Equal(0, result.FoldersDeleted);
        Assert.Equal(0, result.ItemsDeleted);
    }
}
