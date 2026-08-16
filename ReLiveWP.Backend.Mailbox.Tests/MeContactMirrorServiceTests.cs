using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Services;
using ReLiveWP.ServiceDefaults.Events;

namespace ReLiveWP.Backend.Mailbox.Tests;

public class MeContactMirrorServiceTests : IDisposable
{
    private const string UserId = "user-1";
    private const string Cid = "15fe5d7a6d8d65ff";
    private const string ContactsFolderId = "contacts-folder";
    private const string MeContactId = "me-contact";
    private const string OtherContactId = "other-contact";

    private readonly SqliteConnection connection;

    public MeContactMirrorServiceTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var db = NewContext();
        db.Database.EnsureCreated();
        db.Folders.Add(new DbFolder
        {
            Id = ContactsFolderId,
            UserId = UserId,
            DisplayName = "Contacts",
            Type = DbFolderType.ContactsDefault,
        });

        AddContact(db, MeContactId, "wam", contactType: "Me");
        AddContact(db, OtherContactId, "ada", contactType: null);
        db.SaveChanges();
    }

    public void Dispose() => connection.Dispose();

    private static void AddContact(MailboxDbContext db, string id, string firstName, string? contactType)
    {
        var contact = new DbContactItem
        {
            Id = id,
            ServerId = id,
            UserId = UserId,
            CollectionId = ContactsFolderId,
            FirstName = firstName,
            FileAs = firstName,
            Email1Address = "wam@relivewp.net",
            IMAddress = "wam@relivewp.net",
            IMAddress2 = "1:wam@relivewp.net",
        };

        db.Items.Add(contact);
        db.ContactAnnotations.Add(new DbContactAnnotation
        {
            ContactItemId = id,
            ContactItem = contact,
            ContactType = contactType,
        });
    }

    private MailboxDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<MailboxDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new ChangeLogInterceptor())
            .Options;
        return new MailboxDbContext(options);
    }

    private static IConfiguration Config() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["PublicUrls:Avatars"] = "https://login.test/profile" })
        .Build();

    private static MeContactMirrorService NewService(MailboxDbContext db, FakeUserClient users) =>
        new(db, users, Config(), NullLogger<MeContactMirrorService>.Instance);

    private static AccountProfileChangedEvent Event(
        string? first = "Thomas", string? last = "May", string? etag = null) =>
        new(UserId, Cid, first, last, "wam", "wam@relivewp.net", etag);

    [Fact]
    public async Task Mirrors_the_name_onto_the_me_contact()
    {
        await using var db = NewContext();
        await NewService(db, new FakeUserClient()).MirrorAsync(Event());

        var contact = await db.Items.OfType<DbContactItem>().SingleAsync(c => c.Id == MeContactId);
        Assert.Equal("Thomas", contact.FirstName);
        Assert.Equal("May", contact.LastName);
        Assert.Equal("Thomas May", contact.FileAs);
    }

    [Fact]
    public async Task Leaves_other_contacts_alone()
    {
        await using var db = NewContext();
        await NewService(db, new FakeUserClient()).MirrorAsync(Event());

        var other = await db.Items.OfType<DbContactItem>().SingleAsync(c => c.Id == OtherContactId);
        Assert.Equal("ada", other.FirstName);
    }

    [Fact]
    public async Task Falls_back_to_the_username_when_no_name_is_set()
    {
        await using var db = NewContext();
        await NewService(db, new FakeUserClient()).MirrorAsync(Event(first: null, last: null));

        var contact = await db.Items.OfType<DbContactItem>().SingleAsync(c => c.Id == MeContactId);
        Assert.Equal("wam", contact.FirstName);
        Assert.Null(contact.LastName);
        Assert.Equal("wam", contact.FileAs);
    }

    [Fact]
    public async Task Writes_the_tile_url_and_hash_and_picture()
    {
        var users = new FakeUserClient { OnGetUserPicture = _ => FakeUserClient.Picture([1, 2, 3], "abc123") };

        await using var db = NewContext();
        await NewService(db, users).MirrorAsync(Event(etag: "abc123"));

        var contact = await db.Items.OfType<DbContactItem>()
            .Include(c => c.Annotation)
            .SingleAsync(c => c.Id == MeContactId);

        Assert.Equal($"https://login.test/profile/{Cid}/picture", contact.Annotation!.UserTileUrl);
        Assert.Equal("abc123", contact.Annotation.UserTileHash);
        Assert.Equal([1, 2, 3], contact.Picture);
    }

    [Fact]
    public async Task Clears_the_picture_when_the_account_has_none()
    {
        await using (var seed = NewContext())
        {
            var contact = await seed.Items.OfType<DbContactItem>().SingleAsync(c => c.Id == MeContactId);
            contact.Picture = [9, 9, 9];
            await seed.SaveChangesAsync();
        }

        await using var db = NewContext();
        await NewService(db, new FakeUserClient()).MirrorAsync(Event(etag: null));

        var updated = await db.Items.OfType<DbContactItem>()
            .Include(c => c.Annotation)
            .SingleAsync(c => c.Id == MeContactId);

        Assert.Null(updated.Picture);
        Assert.Null(updated.Annotation!.UserTileUrl);
    }

    // the loop breaker: a republish of the same profile must not wake the device
    [Fact]
    public async Task Second_mirror_of_the_same_profile_emits_no_item_event()
    {
        var users = new FakeUserClient { OnGetUserPicture = _ => FakeUserClient.Picture([1, 2, 3], "abc123") };
        var evt = Event(etag: "abc123");

        await using (var first = NewContext())
            await NewService(first, users).MirrorAsync(evt);

        await using var db = NewContext();
        var before = await db.ItemEvents.CountAsync(e => e.ServerId == MeContactId);

        await NewService(db, users).MirrorAsync(evt);

        var after = await db.ItemEvents.CountAsync(e => e.ServerId == MeContactId);
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Mirror_emits_one_item_event_when_something_changed()
    {
        await using var db = NewContext();
        var before = await db.ItemEvents.CountAsync(e => e.ServerId == MeContactId);

        await NewService(db, new FakeUserClient()).MirrorAsync(Event());

        var after = await db.ItemEvents.CountAsync(e => e.ServerId == MeContactId);
        Assert.Equal(before + 1, after);
    }

    [Fact]
    public async Task Does_nothing_when_there_is_no_me_contact()
    {
        await using (var seed = NewContext())
        {
            var annotation = await seed.ContactAnnotations.SingleAsync(a => a.ContactItemId == MeContactId);
            annotation.ContactType = null;
            await seed.SaveChangesAsync();
        }

        await using var db = NewContext();
        await NewService(db, new FakeUserClient()).MirrorAsync(Event());

        var contact = await db.Items.OfType<DbContactItem>().SingleAsync(c => c.Id == MeContactId);
        Assert.Equal("wam", contact.FirstName);
    }
}
