using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Services;
using ReLiveWP.Backend.Mailbox.Services.Grpc;

namespace ReLiveWP.Backend.Mailbox.Tests;

// wires both interceptors in production order so a test sees exactly the save pipeline a real
// request would, validation included
public abstract class MailboxStoreTestBase : IDisposable
{
    protected const string UserId = "user-1";
    protected const string OtherUserId = "user-2";

    private readonly SqliteConnection _connection;
    private readonly SelectCountingInterceptor _selects = new();

    protected int SelectsExecuted => _selects.Selects;

    protected void ResetSelectCount() => _selects.Reset();

    protected MailboxStoreTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    protected MailboxDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<MailboxDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new ItemValidationInterceptor(), new ChangeLogInterceptor(), _selects)
            .Options;
        return new MailboxDbContext(options);
    }

    protected static MailboxStoreService NewService(MailboxDbContext db) =>
        new(db, new MailboxIntegrityService(db), new SyncStateRepairService(db),
            new MailboxDeletionService(db), FakeUserClient.NoLinks(db));

    protected static ServerCallContext NewCallContext() => new StubCallContext();

    protected static string NewId() => Guid.NewGuid().ToString("N");

    protected async Task SeedFolderAsync(string id, DbFolderType type, string parentId = "0", string userId = UserId)
    {
        await using var db = NewContext();
        db.Folders.Add(new DbFolder
        {
            Id = id,
            UserId = userId,
            ParentServerId = parentId,
            DisplayName = id,
            Type = type,
        });
        await db.SaveChangesAsync();
    }

    protected async Task<string> SeedItemAsync(DbItem item)
    {
        var id = item.Id ?? NewId();
        item.Id = id;
        item.ServerId ??= id;
        item.UserId ??= UserId;

        await using var db = NewContext();
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return item.ServerId;
    }

    protected async Task<List<DbItemEvent>> ItemEventsForAsync(string serverId)
    {
        await using var db = NewContext();
        return await db.ItemEvents.Where(e => e.ServerId == serverId).OrderBy(e => e.Id).ToListAsync();
    }

    protected async Task<int> CountItemEventsAsync(DbChangeEventType type, params string[] serverIds)
    {
        await using var db = NewContext();
        return await db.ItemEvents.CountAsync(e => e.EventType == type && serverIds.Contains(e.ServerId));
    }

    protected async Task<int> CountFolderEventsAsync(DbChangeEventType type, params string[] serverIds)
    {
        await using var db = NewContext();
        return await db.FolderEvents.CountAsync(e => e.EventType == type && serverIds.Contains(e.ServerId));
    }
}
