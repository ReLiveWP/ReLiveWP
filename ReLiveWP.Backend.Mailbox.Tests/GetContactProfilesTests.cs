using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mailbox.Tests;

// what the people hub shows on a contact card, resolved from the annotation cid
public class GetContactProfilesTests : IDisposable
{
    private const string UserA = "user-a";
    private const string UserB = "user-b";
    private const long Cid = 0x15fe5d7a6d8d65ff;

    private readonly SqliteConnection connection;

    public GetContactProfilesTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var db = NewContext();
        db.Database.EnsureCreated();

        AddFolder(db, UserA);
        AddFolder(db, UserB);
        db.SaveChanges();
    }

    public void Dispose() => connection.Dispose();

    [Fact]
    public async Task A_linked_contact_resolves_to_its_name_and_tile()
    {
        using var db = NewContext();
        AddContact(db, "a-sees-b", UserA, Cid, fileAs: "Bee Example", tileUrl: "https://relivewp.test/profile/x/picture");
        await db.SaveChangesAsync();

        var profile = Assert.Single((await ProfilesAsync(db, UserA, Cid)).Profiles);

        Assert.Equal(Cid, profile.Cid);
        Assert.Equal("Bee Example", profile.DisplayName);
        Assert.Equal("https://relivewp.test/profile/x/picture", profile.AvatarUrl);
    }

    [Fact]
    public async Task A_contact_with_no_file_as_falls_back_to_its_name_parts()
    {
        using var db = NewContext();
        AddContact(db, "a-sees-b", UserA, Cid, firstName: "bee", lastName: "example");
        await db.SaveChangesAsync();

        var profile = Assert.Single((await ProfilesAsync(db, UserA, Cid)).Profiles);

        Assert.Equal("bee example", profile.DisplayName);
        Assert.False(profile.HasAvatarUrl);
    }

    [Fact]
    public async Task Another_users_contact_does_not_resolve()
    {
        using var db = NewContext();
        AddContact(db, "b-sees-c", UserB, Cid, fileAs: "Cee");
        await db.SaveChangesAsync();

        Assert.Empty((await ProfilesAsync(db, UserA, Cid)).Profiles);
    }

    [Fact]
    public async Task A_deleted_contact_does_not_resolve()
    {
        using var db = NewContext();
        var contact = AddContact(db, "a-sees-b", UserA, Cid, fileAs: "Bee");
        contact.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        Assert.Empty((await ProfilesAsync(db, UserA, Cid)).Profiles);
    }

    [Fact]
    public async Task An_unannotated_contact_does_not_resolve()
    {
        using var db = NewContext();
        AddContact(db, "a-sees-b", UserA, cid: null, fileAs: "Bee");
        await db.SaveChangesAsync();

        Assert.Empty((await ProfilesAsync(db, UserA, Cid)).Profiles);
    }

    private static Task<GetContactProfilesResponse> ProfilesAsync(MailboxDbContext db, string userId, params long[] cids)
    {
        var request = new GetContactProfilesRequest { UserId = userId };
        request.Cids.AddRange(cids);

        return new MailboxStoreService(db, null!, null!, null!, FakeUserClient.NoLinks(db))
            .GetContactProfiles(request, new StubCallContext());
    }

    private static void AddFolder(MailboxDbContext db, string userId) =>
        db.Folders.Add(new DbFolder
        {
            Id = userId + "-contacts",
            UserId = userId,
            DisplayName = "Contacts",
            Type = DbFolderType.ContactsDefault,
        });

    private static DbContactItem AddContact(
        MailboxDbContext db, string id, string userId, long? cid,
        string? fileAs = null, string? firstName = null, string? lastName = null, string? tileUrl = null)
    {
        var contact = new DbContactItem
        {
            Id = id,
            ServerId = id,
            UserId = userId,
            CollectionId = userId + "-contacts",
            FileAs = fileAs,
            FirstName = firstName,
            LastName = lastName,
        };

        db.Items.Add(contact);
        db.ContactAnnotations.Add(new DbContactAnnotation
        {
            ContactItemId = contact.Id,
            ContactItem = contact,
            Cid = cid,
            UserTileUrl = tileUrl,
        });

        return contact;
    }

    private MailboxDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<MailboxDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new ChangeLogInterceptor())
            .Options;
        return new MailboxDbContext(options);
    }
}
