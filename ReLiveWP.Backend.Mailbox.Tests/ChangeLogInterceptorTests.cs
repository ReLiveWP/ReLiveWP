using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;

namespace ReLiveWP.Backend.Mailbox.Tests;

// what DbItemEvent/DbFolderEvent rows a mutation produces. SQLite (not InMemory) so the interceptor
// sees the same modified-property set production would.
public class ChangeLogInterceptorTests : IDisposable
{
    private const string TasksFolderId = "tasks-folder";
    private const string ContactsFolderId = "contacts-folder";
    private const string UserId = "user-1";

    private readonly SqliteConnection _connection;

    public ChangeLogInterceptorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated();
        db.Folders.AddRange(
            new DbFolder { Id = TasksFolderId, UserId = UserId, DisplayName = "Tasks", Type = DbFolderType.TasksDefault },
            new DbFolder { Id = ContactsFolderId, UserId = UserId, DisplayName = "Contacts", Type = DbFolderType.ContactsDefault });
        db.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private MailboxDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<MailboxDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new ChangeLogInterceptor())
            .Options;
        return new MailboxDbContext(options);
    }

    private static DbTask NewTask(string id, string collectionId = TasksFolderId) => new()
    {
        Id = id,
        ServerId = id,
        UserId = UserId,
        CollectionId = collectionId,
    };

    [Fact]
    public async Task Add_emits_Add_event()
    {
        await using var db = NewContext();
        db.Items.Add(NewTask("t1"));
        await db.SaveChangesAsync();

        var events = await db.ItemEvents.Where(e => e.ServerId == "t1").ToListAsync();
        var evt = Assert.Single(events);
        Assert.Equal(DbChangeEventType.Add, evt.EventType);
        Assert.Equal(TasksFolderId, evt.CollectionId);
    }

    [Fact]
    public async Task Update_emits_Update_event()
    {
        await using (var seed = NewContext())
        {
            seed.Items.Add(NewTask("t2"));
            await seed.SaveChangesAsync();
        }

        await using var db = NewContext();
        var task = await db.Items.OfType<DbTask>().SingleAsync(t => t.Id == "t2");
        task.Subject = "changed";
        await db.SaveChangesAsync();

        var events = await db.ItemEvents.Where(e => e.ServerId == "t2").OrderBy(e => e.Id).ToListAsync();
        Assert.Equal(2, events.Count); // the initial Add plus this Update
        Assert.Equal(DbChangeEventType.Update, events[^1].EventType);
    }

    [Fact]
    public async Task Move_emits_Delete_at_old_and_Add_at_new_collection()
    {
        await using (var seed = NewContext())
        {
            seed.Items.Add(NewTask("t3"));
            await seed.SaveChangesAsync();
        }

        await using var db = NewContext();
        var task = await db.Items.OfType<DbTask>().SingleAsync(t => t.Id == "t3");
        task.CollectionId = ContactsFolderId;
        await db.SaveChangesAsync();

        var events = await db.ItemEvents.Where(e => e.ServerId == "t3" && e.OccurredAt > DateTime.MinValue)
            .OrderBy(e => e.Id).ToListAsync();
        var moveEvents = events.TakeLast(2).ToList();
        Assert.Equal(DbChangeEventType.Delete, moveEvents[0].EventType);
        Assert.Equal(TasksFolderId, moveEvents[0].CollectionId);
        Assert.Equal(DbChangeEventType.Add, moveEvents[1].EventType);
        Assert.Equal(ContactsFolderId, moveEvents[1].CollectionId);
    }

    [Fact]
    public async Task SoftDelete_emits_Delete_event()
    {
        await using (var seed = NewContext())
        {
            seed.Items.Add(NewTask("t4"));
            await seed.SaveChangesAsync();
        }

        await using var db = NewContext();
        var task = await db.Items.OfType<DbTask>().SingleAsync(t => t.Id == "t4");
        task.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var last = await db.ItemEvents.Where(e => e.ServerId == "t4").OrderBy(e => e.Id).LastAsync();
        Assert.Equal(DbChangeEventType.Delete, last.EventType);
    }

    [Fact]
    public async Task ValidationFlag_transition_emits_Delete_event()
    {
        // guards against quarantine falling through to a generic Update, which GetItems' filter would swallow
        await using (var seed = NewContext())
        {
            seed.Items.Add(NewTask("t5"));
            await seed.SaveChangesAsync();
        }

        await using var db = NewContext();
        var task = await db.Items.OfType<DbTask>().SingleAsync(t => t.Id == "t5");
        task.ValidationFlaggedAt = DateTime.UtcNow;
        task.ValidationReason = "folder-class-congruence";
        await db.SaveChangesAsync();

        var last = await db.ItemEvents.Where(e => e.ServerId == "t5").OrderBy(e => e.Id).LastAsync();
        Assert.Equal(DbChangeEventType.Delete, last.EventType);
    }

    [Fact]
    public async Task ValidationFlag_release_emits_Add_event()
    {
        await using (var seed = NewContext())
        {
            var task = NewTask("t6");
            task.ValidationFlaggedAt = DateTime.UtcNow;
            task.ValidationReason = "folder-class-congruence";
            seed.Items.Add(task);
            await seed.SaveChangesAsync();
        }

        await using var db = NewContext();
        var released = await db.Items.OfType<DbTask>().SingleAsync(t => t.Id == "t6");
        released.ValidationFlaggedAt = null;
        released.ValidationReason = null;
        await db.SaveChangesAsync();

        var last = await db.ItemEvents.Where(e => e.ServerId == "t6").OrderBy(e => e.Id).LastAsync();
        Assert.Equal(DbChangeEventType.Add, last.EventType);
    }

    [Fact]
    public async Task SoftDelete_and_ValidationFlag_together_prefers_Delete()
    {
        // a quarantined item deleted in the same batch must never look "released" (Add) to a device
        await using (var seed = NewContext())
        {
            var task = NewTask("t7");
            task.ValidationFlaggedAt = DateTime.UtcNow;
            task.ValidationReason = "folder-class-congruence";
            seed.Items.Add(task);
            await seed.SaveChangesAsync();
        }

        await using var db = NewContext();
        var task2 = await db.Items.OfType<DbTask>().SingleAsync(t => t.Id == "t7");
        task2.ValidationFlaggedAt = null;
        task2.ValidationReason = null;
        task2.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var events = await db.ItemEvents.Where(e => e.ServerId == "t7").OrderBy(e => e.Id).ToListAsync();
        Assert.Equal(2, events.Count); // seed Add, then a single Delete -- no Add for the release
        Assert.Equal(DbChangeEventType.Delete, events[^1].EventType);
    }

    [Fact]
    public async Task Move_and_ValidationFlag_together_prefers_move_semantics()
    {
        await using (var seed = NewContext())
        {
            seed.Items.Add(NewTask("t8"));
            await seed.SaveChangesAsync();
        }

        await using var db = NewContext();
        var task = await db.Items.OfType<DbTask>().SingleAsync(t => t.Id == "t8");
        task.CollectionId = ContactsFolderId;
        task.ValidationFlaggedAt = DateTime.UtcNow;
        task.ValidationReason = "folder-class-congruence";
        await db.SaveChangesAsync();

        var events = await db.ItemEvents.Where(e => e.ServerId == "t8").OrderBy(e => e.Id).ToListAsync();
        var moveEvents = events.TakeLast(2).ToList();
        Assert.Equal(DbChangeEventType.Delete, moveEvents[0].EventType);
        Assert.Equal(TasksFolderId, moveEvents[0].CollectionId);
        Assert.Equal(DbChangeEventType.Add, moveEvents[1].EventType);
        Assert.Equal(ContactsFolderId, moveEvents[1].CollectionId);
    }

    [Fact]
    public async Task ChildMutation_bubbles_to_parent_Update()
    {
        await using (var seed = NewContext())
        {
            seed.Items.Add(new DbContactItem
            {
                Id = "c1",
                ServerId = "c1",
                UserId = UserId,
                CollectionId = ContactsFolderId,
                FirstName = "Ada",
            });
            await seed.SaveChangesAsync();
        }

        await using var db = NewContext();
        var contact = await db.Items.OfType<DbContactItem>().SingleAsync(c => c.Id == "c1");
        db.ContactCategories.Add(new DbContactCategory
        {
            Id = "cat1",
            ContactItemId = contact.Id,
            Name = "Friends",
        });
        await db.SaveChangesAsync();

        var last = await db.ItemEvents.Where(e => e.ServerId == "c1").OrderBy(e => e.Id).LastAsync();
        Assert.Equal(DbChangeEventType.Update, last.EventType);
    }

    [Fact]
    public async Task FolderAdd_emits_FolderEvent_Add()
    {
        await using var db = NewContext();
        db.Folders.Add(new DbFolder
        {
            Id = "new-folder",
            UserId = UserId,
            DisplayName = "New",
            Type = DbFolderType.Generic,
        });
        await db.SaveChangesAsync();

        var evt = await db.FolderEvents.SingleAsync(e => e.ServerId == "new-folder");
        Assert.Equal(DbChangeEventType.Add, evt.EventType);
    }

    [Fact]
    public async Task FolderSoftDelete_emits_FolderEvent_Delete()
    {
        await using var db = NewContext();
        var folder = await db.Folders.SingleAsync(f => f.Id == TasksFolderId);
        folder.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var last = await db.FolderEvents.Where(e => e.ServerId == TasksFolderId).OrderBy(e => e.Id).LastAsync();
        Assert.Equal(DbChangeEventType.Delete, last.EventType);
    }
}
