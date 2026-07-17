using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Validation;

namespace ReLiveWP.Backend.Mailbox.Tests;

// covers the wiring the pure rule tests can't: that a rejection aborts before anything persists, and
// that a correction made inside SavingChanges still reaches the DB despite EF already having computed
// the modified-column set. SQLite, not InMemory, since InMemory replaces whole rows and would pass
// regardless of whether the interceptor gets that right.
public class ItemValidationInterceptorTests : IDisposable
{
    private const string TasksFolderId = "tasks-folder";
    private const string InboxFolderId = "inbox-folder";
    private const string UserId = "user-1";

    // held open for the fixture's lifetime; the in-memory database dies with the last connection
    private readonly SqliteConnection _connection;

    public ItemValidationInterceptorTests()
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
            .AddInterceptors(new ItemValidationInterceptor())
            .Options;
        return new MailboxDbContext(options);
    }

    private async Task<MailboxDbContext> SeededContextAsync()
    {
        var db = NewContext();
        if (!await db.Folders.AnyAsync())
        {
            db.Folders.AddRange(
                new DbFolder { Id = TasksFolderId, UserId = UserId, DisplayName = "Tasks", Type = DbFolderType.TasksDefault },
                new DbFolder { Id = InboxFolderId, UserId = UserId, DisplayName = "Inbox", Type = DbFolderType.InboxDefault });
            await db.SaveChangesAsync();
        }
        return db;
    }

    private static DbTask NewTask(string id) => new()
    {
        Id = id,
        ServerId = id,
        UserId = UserId,
        CollectionId = TasksFolderId,
    };

    [Fact]
    public async Task Correction_applied_during_save_is_persisted_on_insert()
    {
        await using var db = await SeededContextAsync();

        var task = NewTask("t1");
        task.Complete = true;
        task.DateCompleted = null;
        db.Items.Add(task);
        await db.SaveChangesAsync();

        await using var verify = NewContext();
        var saved = await verify.Items.OfType<DbTask>().SingleAsync(t => t.Id == "t1");
        Assert.True(saved.Complete);
        Assert.NotNull(saved.DateCompleted);
    }

    [Fact]
    public async Task Correction_applied_during_save_is_persisted_on_update()
    {
        await using (var seed = await SeededContextAsync())
        {
            var task = NewTask("t2");
            task.Subject = "before";
            seed.Items.Add(task);
            await seed.SaveChangesAsync();
        }

        await using (var db = NewContext())
        {
            var task = await db.Items.OfType<DbTask>().SingleAsync(t => t.Id == "t2");
            task.Subject = "after";
            task.Complete = true;
            await db.SaveChangesAsync();
        }

        await using var verify = NewContext();
        var saved = await verify.Items.OfType<DbTask>().SingleAsync(t => t.Id == "t2");
        Assert.Equal("after", saved.Subject);
        Assert.NotNull(saved.DateCompleted);
    }

    [Fact]
    public async Task Item_in_the_wrong_folder_is_refused_and_nothing_persists()
    {
        await using var db = await SeededContextAsync();

        db.Items.Add(new DbContactItem
        {
            Id = "c1",
            ServerId = "c1",
            UserId = UserId,
            CollectionId = TasksFolderId, // a contact in a Tasks folder
            FirstName = "Ada",
        });

        var ex = await Assert.ThrowsAsync<ItemValidationException>(() => db.SaveChangesAsync());
        Assert.Contains(ex.Issues, i => i.Rule == "folder-class-congruence");

        db.ChangeTracker.Clear();
        await using var verify = NewContext();
        Assert.False(await verify.Items.AnyAsync(i => i.Id == "c1"));
    }

    [Fact]
    public async Task A_rejected_item_takes_down_its_whole_batch_but_not_a_separate_one()
    {
        await using var db = await SeededContextAsync();

        db.Items.Add(NewTask("good"));
        db.Items.Add(new DbEmail
        {
            Id = "bad",
            ServerId = "bad",
            UserId = UserId,
            CollectionId = TasksFolderId, // email in a Tasks folder
        });
        await Assert.ThrowsAsync<ItemValidationException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        await using var db2 = NewContext();
        db2.Items.Add(NewTask("good"));
        await db2.SaveChangesAsync();

        await using var verify = NewContext();
        Assert.True(await verify.Items.AnyAsync(i => i.Id == "good"));
        Assert.False(await verify.Items.AnyAsync(i => i.Id == "bad"));
    }

    [Fact]
    public async Task A_quarantined_item_is_saved_without_being_revalidated()
    {
        await using var db = await SeededContextAsync();

        db.Items.Add(new DbEmail
        {
            Id = "flagged",
            ServerId = "flagged",
            UserId = UserId,
            CollectionId = TasksFolderId, // would otherwise be rejected
            ValidationFlaggedAt = DateTime.UtcNow,
            ValidationReason = "folder-class-congruence",
        });

        await db.SaveChangesAsync();

        await using var verify = NewContext();
        Assert.True(await verify.Items.AnyAsync(i => i.Id == "flagged"));
    }

    [Fact]
    public async Task Folder_type_is_resolved_when_the_folder_is_not_already_tracked()
    {
        await using (var seed = await SeededContextAsync()) { }

        await using var db = NewContext();
        Assert.Empty(db.Folders.Local); // nothing tracked, so the lookup must hit the database

        db.Items.Add(new DbContactItem
        {
            Id = "c2",
            ServerId = "c2",
            UserId = UserId,
            CollectionId = TasksFolderId,
            FirstName = "Ada",
        });

        var ex = await Assert.ThrowsAsync<ItemValidationException>(() => db.SaveChangesAsync());
        Assert.Contains(ex.Issues, i => i.Rule == "folder-class-congruence");
    }

    [Fact]
    public async Task An_item_in_an_unknown_folder_is_left_to_the_foreign_key()
    {
        await using (var seed = await SeededContextAsync()) { }

        await using var db = NewContext();
        db.Items.Add(new DbTask
        {
            Id = "t3",
            ServerId = "t3",
            UserId = UserId,
            CollectionId = "no-such-folder",
        });

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task A_valid_item_saves_untouched()
    {
        await using var db = await SeededContextAsync();

        db.Items.Add(new DbEmail
        {
            Id = "e1",
            ServerId = "e1",
            UserId = UserId,
            CollectionId = InboxFolderId,
            Subject = "hello",
        });
        await db.SaveChangesAsync();

        await using var verify = NewContext();
        var saved = await verify.Items.OfType<DbEmail>().SingleAsync(e => e.Id == "e1");
        Assert.Equal("hello", saved.Subject);
        Assert.Null(saved.ValidationFlaggedAt);
    }
}
