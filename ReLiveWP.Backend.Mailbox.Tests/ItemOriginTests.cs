using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Services;
using ReLiveWP.Backend.Mailbox.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mailbox.Tests;

// Mirrored contacts share the user's real address book, so provenance is the only thing separating
// "this row came from Google" from "the user typed this in". Everything here is about that boundary
// holding under deletion, and about RemoteSynced changing hands at the right moment.
public class ItemOriginTests : IDisposable
{
    private const string ContactsFolderId = "contacts-folder";
    private const string UserId = "user-1";
    private const string Service = "google";
    private const string RemoteCollection = "people/me";

    private readonly SqliteConnection _connection;

    public ItemOriginTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated();
        db.Folders.Add(new DbFolder
        {
            Id = ContactsFolderId,
            UserId = UserId,
            DisplayName = "Contacts",
            Type = DbFolderType.ContactsDefault,
        });
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

    private MailboxStoreService NewService(MailboxDbContext db) =>
        new(db, new MailboxIntegrityService(db), new SyncStateRepairService(db),
            new MailboxDeletionService(db), FakeUserClient.NoLinks(db));

    private static ServerCallContext NewCallContext() => new StubCallContext();

    private static ItemOrigin OriginFor(string externalId) => new()
    {
        ServiceId = Service,
        CollectionId = RemoteCollection,
        ExternalId = externalId,
        Etag = $"etag-{externalId}",
    };

    private static CreateItemRequest MirroredContact(string first, string externalId) => new()
    {
        UserId = UserId,
        CollectionId = ContactsFolderId,
        Contact = new ContactItem { FirstName = first, FileAs = first },
        Origin = OriginFor(externalId),
    };

    private static CreateItemRequest NativeContact(string first) => new()
    {
        UserId = UserId,
        CollectionId = ContactsFolderId,
        Contact = new ContactItem { FirstName = first, FileAs = first },
    };

    private async Task<string> SeedAsync(CreateItemRequest request)
    {
        using var db = NewContext();
        var item = await NewService(db).CreateItem(request, NewCallContext());
        return item.ServerId;
    }

    private async Task<Item> ReadAsync(string serverId)
    {
        using var db = NewContext();
        return await NewService(db).GetItem(
            new GetItemRequest { UserId = UserId, ServerId = serverId }, NewCallContext());
    }

    [Fact]
    public async Task Origin_round_trips_through_create_and_read()
    {
        var serverId = await SeedAsync(MirroredContact("Ada", "c1"));

        var read = await ReadAsync(serverId);

        Assert.NotNull(read.Origin);
        Assert.Equal(Service, read.Origin.ServiceId);
        Assert.Equal(RemoteCollection, read.Origin.CollectionId);
        Assert.Equal("c1", read.Origin.ExternalId);
        Assert.Equal("etag-c1", read.Origin.Etag);
        Assert.True(read.Origin.RemoteSynced);
    }

    // the origin has to outlive the edit: it is what a later pull matches on, and without it the
    // provider's copy arrives again as a second contact
    [Fact]
    public async Task Update_without_origin_leaves_the_stored_origin_alone()
    {
        var serverId = await SeedAsync(MirroredContact("Ada", "c1"));

        using (var db = NewContext())
        {
            await NewService(db).UpdateItem(new UpdateItemRequest
            {
                UserId = UserId,
                ServerId = serverId,
                Contact = new ContactItem { FirstName = "Ada", LastName = "Lovelace", FileAs = "Lovelace, Ada" },
            }, NewCallContext());
        }

        var read = await ReadAsync(serverId);

        Assert.Equal("Lovelace", read.Contact.LastName);
        Assert.NotNull(read.Origin);
        Assert.Equal("c1", read.Origin.ExternalId);
    }

    [Fact]
    public async Task An_edit_from_anywhere_but_the_mirror_stops_tracking()
    {
        var serverId = await SeedAsync(MirroredContact("Ada", "c1"));

        using (var db = NewContext())
        {
            await NewService(db).UpdateItem(new UpdateItemRequest
            {
                UserId = UserId,
                ServerId = serverId,
                Contact = new ContactItem { FirstName = "Ada", LastName = "Lovelace", FileAs = "Lovelace, Ada" },
            }, NewCallContext());
        }

        var read = await ReadAsync(serverId);

        Assert.NotNull(read.Origin);
        Assert.False(read.Origin.RemoteSynced);
    }

    [Fact]
    public async Task A_mirror_update_keeps_tracking()
    {
        var serverId = await SeedAsync(MirroredContact("Ada", "c1"));

        using (var db = NewContext())
        {
            await NewService(db).UpdateItem(new UpdateItemRequest
            {
                UserId = UserId,
                ServerId = serverId,
                Contact = new ContactItem { FirstName = "Ada", LastName = "Lovelace", FileAs = "Lovelace, Ada" },
                Origin = OriginFor("c1"),
            }, NewCallContext());
        }

        var read = await ReadAsync(serverId);

        Assert.Equal("Lovelace", read.Contact.LastName);
        Assert.True(read.Origin.RemoteSynced);
    }

    // WP7 resends Changes it has already sent. Reacting to the request rather than to a real change
    // would freeze those contacts for good, so the no-op case is the one worth pinning down.
    [Fact]
    public async Task A_change_that_alters_nothing_keeps_tracking()
    {
        var serverId = await SeedAsync(MirroredContact("Ada", "c1"));

        using (var db = NewContext())
        {
            await NewService(db).UpdateItem(new UpdateItemRequest
            {
                UserId = UserId,
                ServerId = serverId,
                Contact = new ContactItem { FirstName = "Ada", FileAs = "Ada" },
            }, NewCallContext());
        }

        var read = await ReadAsync(serverId);

        Assert.True(read.Origin.RemoteSynced);
    }

    // a mirror write is also how an import that replaces your changes puts a contact back
    [Fact]
    public async Task A_mirror_write_takes_an_edited_contact_back()
    {
        var serverId = await SeedAsync(MirroredContact("Ada", "c1"));

        using (var db = NewContext())
        {
            await NewService(db).UpdateItem(new UpdateItemRequest
            {
                UserId = UserId,
                ServerId = serverId,
                Contact = new ContactItem { FirstName = "Addie", FileAs = "Addie" },
            }, NewCallContext());
        }

        Assert.False((await ReadAsync(serverId)).Origin.RemoteSynced);

        using (var db = NewContext())
        {
            await NewService(db).UpdateItem(new UpdateItemRequest
            {
                UserId = UserId,
                ServerId = serverId,
                Contact = new ContactItem { FirstName = "Ada", FileAs = "Ada" },
                Origin = OriginFor("c1"),
            }, NewCallContext());
        }

        Assert.True((await ReadAsync(serverId)).Origin.RemoteSynced);
    }

    [Fact]
    public async Task Native_contacts_have_no_origin()
    {
        var serverId = await SeedAsync(NativeContact("Typed By Hand"));

        var read = await ReadAsync(serverId);

        Assert.Null(read.Origin);
    }

    [Fact]
    public async Task Native_contacts_are_not_tracked()
    {
        await SeedAsync(NativeContact("Typed By Hand"));

        using var verify = NewContext();
        var contact = await verify.Items.OfType<DbContactItem>().SingleAsync();

        Assert.False(contact.RemoteSynced);
    }

    [Fact]
    public async Task Delete_by_origin_leaves_native_contacts_untouched()
    {
        await SeedAsync(MirroredContact("Ada", "c1"));
        await SeedAsync(MirroredContact("Grace", "c2"));
        var nativeId = await SeedAsync(NativeContact("Mum"));

        using (var db = NewContext())
        {
            var result = await NewService(db).DeleteItemsByOrigin(new DeleteItemsByOriginRequest
            {
                UserId = UserId,
                OriginServiceId = Service,
            }, NewCallContext());

            Assert.Equal(2, result.ItemsDeleted);
        }

        using var verify = NewContext();
        var live = await verify.Items.OfType<DbContactItem>()
            .Where(i => i.UserId == UserId && i.DeletedAt == null)
            .Select(i => i.ServerId)
            .ToListAsync();

        Assert.Equal([nativeId], live);
    }

    [Fact]
    public async Task Delete_by_origin_emits_one_change_event_per_item()
    {
        await SeedAsync(MirroredContact("Ada", "c1"));
        await SeedAsync(MirroredContact("Grace", "c2"));

        using (var db = NewContext())
        {
            await NewService(db).DeleteItemsByOrigin(new DeleteItemsByOriginRequest
            {
                UserId = UserId,
                OriginServiceId = Service,
            }, NewCallContext());
        }

        // a bulk ExecuteUpdate would pass every row-state check above and emit nothing here, which
        // is precisely the shape of a silent device desync
        using var verify = NewContext();
        var deletes = await verify.ItemEvents
            .Where(e => e.UserId == UserId && e.EventType == DbChangeEventType.Delete)
            .CountAsync();

        Assert.Equal(2, deletes);
    }

    [Fact]
    public async Task Delete_by_origin_can_target_specific_external_ids()
    {
        await SeedAsync(MirroredContact("Ada", "c1"));
        await SeedAsync(MirroredContact("Grace", "c2"));

        using (var db = NewContext())
        {
            var result = await NewService(db).DeleteItemsByOrigin(new DeleteItemsByOriginRequest
            {
                UserId = UserId,
                OriginServiceId = Service,
                ExternalIds = { "c2" },
            }, NewCallContext());

            Assert.Equal(1, result.ItemsDeleted);
        }

        using var verify = NewContext();
        var survivor = await verify.Items.OfType<DbContactItem>()
            .SingleAsync(i => i.DeletedAt == null);

        Assert.Equal("c1", survivor.OriginExternalId);
    }

    // removing a connection deliberately takes everything it put here, edited contacts included:
    // deciding what a remote delete may touch is the sync's job, not this one's
    [Fact]
    public async Task Connection_removal_deletes_the_whole_origin()
    {
        await SeedAsync(MirroredContact("Ada", "c1"));
        var editedId = await SeedAsync(MirroredContact("Grace", "c2"));

        using (var db = NewContext())
        {
            await NewService(db).UpdateItem(new UpdateItemRequest
            {
                UserId = UserId,
                ServerId = editedId,
                Contact = new ContactItem { FirstName = "Grace", LastName = "Hopper", FileAs = "Hopper, Grace" },
            }, NewCallContext());
        }

        using var db2 = NewContext();
        var result = await NewService(db2).DeleteItemsByOrigin(new DeleteItemsByOriginRequest
        {
            UserId = UserId,
            OriginServiceId = Service,
        }, NewCallContext());

        Assert.Equal(2, result.ItemsDeleted);
    }

    [Fact]
    public async Task Delete_by_origin_ignores_other_services()
    {
        await SeedAsync(MirroredContact("Ada", "c1"));

        using (var db = NewContext())
        {
            await NewService(db).CreateItem(new CreateItemRequest
            {
                UserId = UserId,
                CollectionId = ContactsFolderId,
                Contact = new ContactItem { FirstName = "Dav", FileAs = "Dav" },
                Origin = new ItemOrigin
                {
                    ServiceId = "carddav",
                    CollectionId = "https://dav.example/addressbooks/personal/",
                    ExternalId = "dav-1",
                },
            }, NewCallContext());
        }

        using (var db = NewContext())
        {
            var result = await NewService(db).DeleteItemsByOrigin(new DeleteItemsByOriginRequest
            {
                UserId = UserId,
                OriginServiceId = Service,
            }, NewCallContext());

            Assert.Equal(1, result.ItemsDeleted);
        }

        using var verify = NewContext();
        var survivor = await verify.Items.OfType<DbContactItem>().SingleAsync(i => i.DeletedAt == null);
        Assert.Equal("carddav", survivor.OriginServiceId);
    }
}
