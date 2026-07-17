using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Services;

namespace ReLiveWP.Backend.Mailbox.Tests;

// the retroactive sweep over pre-validation data: fix what's fixable, quarantine what isn't
public class MailboxIntegrityServiceTests : IDisposable
{
    private const string TasksFolderId = "tasks-folder";
    private const string InboxFolderId = "inbox-folder";
    private const string UserId = "user-1";

    private readonly SqliteConnection _connection;

    public MailboxIntegrityServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated();
        db.Folders.AddRange(
            new DbFolder { Id = TasksFolderId, UserId = UserId, DisplayName = "Tasks", Type = DbFolderType.TasksDefault },
            new DbFolder { Id = InboxFolderId, UserId = UserId, DisplayName = "Inbox", Type = DbFolderType.InboxDefault });
        db.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    // no validation interceptor: seeds rows that predate it, like a database corrupted by an earlier bug
    private MailboxDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<MailboxDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new MailboxDbContext(options);
    }

    private async Task SeedAsync(params DbItem[] items)
    {
        await using var db = NewContext();
        db.Items.AddRange(items);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Sweep_corrects_a_task_completed_without_a_date()
    {
        await SeedAsync(new DbTask
        {
            Id = "t1",
            ServerId = "t1",
            UserId = UserId,
            CollectionId = TasksFolderId,
            Complete = true,
            DateCompleted = null,
        });

        await using (var db = NewContext())
        {
            var (scanned, corrected, flagged) = await new MailboxIntegrityService(db)
                .SweepAsync(null, null, default);

            Assert.Equal(1, scanned);
            Assert.Equal(1, corrected);
            Assert.Equal(0, flagged);
        }

        await using var verify = NewContext();
        var task = await verify.Items.OfType<DbTask>().SingleAsync(t => t.Id == "t1");
        Assert.NotNull(task.DateCompleted);
        Assert.Null(task.ValidationFlaggedAt);
    }

    [Fact]
    public async Task Sweep_quarantines_an_item_in_the_wrong_folder()
    {
        await SeedAsync(new DbEmail
        {
            Id = "e1",
            ServerId = "e1",
            UserId = UserId,
            CollectionId = TasksFolderId, // an email living in a Tasks folder
        });

        await using (var db = NewContext())
        {
            var (_, _, flagged) = await new MailboxIntegrityService(db).SweepAsync(null, null, default);
            Assert.Equal(1, flagged);
        }

        await using var verify = NewContext();
        var email = await verify.Items.SingleAsync(i => i.Id == "e1");
        Assert.NotNull(email.ValidationFlaggedAt);
        Assert.Contains("folder-class-congruence", email.ValidationReason);
    }

    [Fact]
    public async Task Sweep_releases_an_item_that_has_since_become_valid()
    {
        await SeedAsync(new DbTask
        {
            Id = "t2",
            ServerId = "t2",
            UserId = UserId,
            CollectionId = TasksFolderId,
            ValidationFlaggedAt = DateTime.UtcNow,
            ValidationReason = "stale reason from an earlier sweep",
        });

        await using (var db = NewContext())
            await new MailboxIntegrityService(db).SweepAsync(null, null, default);

        await using var verify = NewContext();
        var task = await verify.Items.SingleAsync(i => i.Id == "t2");
        Assert.Null(task.ValidationFlaggedAt);
        Assert.Null(task.ValidationReason);
    }

    [Fact]
    public async Task Sweep_is_idempotent()
    {
        await SeedAsync(
            new DbEmail { Id = "bad", ServerId = "bad", UserId = UserId, CollectionId = TasksFolderId },
            new DbEmail { Id = "ok", ServerId = "ok", UserId = UserId, CollectionId = InboxFolderId });

        await using (var db = NewContext())
            await new MailboxIntegrityService(db).SweepAsync(null, null, default);

        await using (var db = NewContext())
        {
            var (scanned, corrected, flagged) = await new MailboxIntegrityService(db)
                .SweepAsync(null, null, default);

            Assert.Equal(2, scanned);
            Assert.Equal(0, corrected); // nothing left to fix on a second pass
            Assert.Equal(1, flagged);   // the unfixable one stays quarantined
        }
    }

    [Fact]
    public async Task Sweep_can_be_scoped_to_one_collection()
    {
        await SeedAsync(
            new DbEmail { Id = "bad", ServerId = "bad", UserId = UserId, CollectionId = TasksFolderId },
            new DbEmail { Id = "ok", ServerId = "ok", UserId = UserId, CollectionId = InboxFolderId });

        await using var db = NewContext();
        var (scanned, _, _) = await new MailboxIntegrityService(db).SweepAsync(UserId, InboxFolderId, default);

        Assert.Equal(1, scanned);
    }

    [Fact]
    public async Task Sweep_can_be_scoped_to_one_user()
    {
        await using (var db = NewContext())
        {
            db.Folders.Add(new DbFolder
            {
                Id = "other-inbox",
                UserId = "user-2",
                DisplayName = "Inbox",
                Type = DbFolderType.InboxDefault,
            });
            await db.SaveChangesAsync();
        }

        await SeedAsync(
            new DbEmail { Id = "mine", ServerId = "mine", UserId = UserId, CollectionId = InboxFolderId },
            new DbEmail { Id = "theirs", ServerId = "theirs", UserId = "user-2", CollectionId = "other-inbox" });

        await using var db2 = NewContext();
        var scoped = await new MailboxIntegrityService(db2).SweepAsync(UserId, null, default);
        Assert.Equal(1, scoped.Scanned);

        await using var db3 = NewContext();
        var everything = await new MailboxIntegrityService(db3).SweepAsync(null, null, default);
        Assert.Equal(2, everything.Scanned);
    }

    [Fact]
    public async Task Sweep_leaves_healthy_data_alone()
    {
        await SeedAsync(new DbEmail
        {
            Id = "e2",
            ServerId = "e2",
            UserId = UserId,
            CollectionId = InboxFolderId,
            Subject = "hello",
        });

        await using (var db = NewContext())
        {
            var (scanned, corrected, flagged) = await new MailboxIntegrityService(db)
                .SweepAsync(null, null, default);

            Assert.Equal(1, scanned);
            Assert.Equal(0, corrected);
            Assert.Equal(0, flagged);
        }

        await using var verify = NewContext();
        var email = await verify.Items.OfType<DbEmail>().SingleAsync(e => e.Id == "e2");
        Assert.Equal("hello", email.Subject);
        Assert.Null(email.ValidationFlaggedAt);
    }
}
