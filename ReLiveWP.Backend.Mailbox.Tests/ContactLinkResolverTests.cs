using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Services;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Mailbox.Tests;

public class ContactLinkResolverTests : IDisposable
{
    private const string UserA = "user-a";
    private const string UserB = "user-b";
    private const string EmailA = "ada@relivewp.net";
    private const string EmailB = "wam@relivewp.net";
    private const string CidB = "15fe5d7a6d8d65ff";
    private const long CidBValue = 0x15fe5d7a6d8d65ff;

    private readonly SqliteConnection connection;
    private readonly FakeUserClient users = new();

    public ContactLinkResolverTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var db = NewContext();
        db.Database.EnsureCreated();

        AddFolder(db, UserA);
        AddFolder(db, UserB);
        AddMeContact(db, UserA, EmailA);
        AddMeContact(db, UserB, EmailB);
        db.SaveChanges();
    }

    public void Dispose() => connection.Dispose();

    [Fact]
    public async Task Public_target_links_one_way()
    {
        using var db = NewContext();
        AddPeerContact(db, "a-sees-b", UserA, EmailB);
        await db.SaveChangesAsync();

        await ResolveAsync(db, "a-sees-b", ProfileVisibility.Public);

        var annotation = await db.ContactAnnotations.SingleAsync(a => a.ContactItemId == "a-sees-b");
        Assert.Equal(CidBValue, annotation.Cid);
        Assert.Equal(EmailB, annotation.WLId);
        Assert.Equal("1:" + EmailB, annotation.ImMri);
    }

    [Fact]
    public async Task Mutual_target_links_only_when_reciprocated()
    {
        using var db = NewContext();
        AddPeerContact(db, "a-sees-b", UserA, EmailB);
        await db.SaveChangesAsync();

        await ResolveAsync(db, "a-sees-b", ProfileVisibility.Mutual);
        Assert.Null((await db.ContactAnnotations.SingleOrDefaultAsync(a => a.ContactItemId == "a-sees-b"))?.Cid);

        AddPeerContact(db, "b-sees-a", UserB, EmailA);
        await db.SaveChangesAsync();

        await ResolveAsync(db, "a-sees-b", ProfileVisibility.Mutual);
        Assert.Equal(CidBValue, (await db.ContactAnnotations.SingleAsync(a => a.ContactItemId == "a-sees-b")).Cid);
    }

    // a private user still sees everyone who is discoverable to them; visibility governs how you are
    // found, not what you can find
    [Fact]
    public async Task Private_viewer_still_resolves_a_mutual_target()
    {
        using var db = NewContext();
        AddPeerContact(db, "a-sees-b", UserA, EmailB);
        AddPeerContact(db, "b-sees-a", UserB, EmailA);
        await db.SaveChangesAsync();

        await ResolveAsync(db, "a-sees-b", ProfileVisibility.Mutual);

        Assert.Equal(CidBValue, (await db.ContactAnnotations.SingleAsync(a => a.ContactItemId == "a-sees-b")).Cid);
    }

    [Fact]
    public async Task Link_is_cleared_when_the_target_stops_being_discoverable()
    {
        using var db = NewContext();
        AddPeerContact(db, "a-sees-b", UserA, EmailB);
        await db.SaveChangesAsync();

        await ResolveAsync(db, "a-sees-b", ProfileVisibility.Public);
        Assert.NotNull((await db.ContactAnnotations.SingleAsync(a => a.ContactItemId == "a-sees-b")).Cid);

        users.OnLookupUsersByEmail = _ => new LookupUsersByEmailResponse();
        await NewResolver(db).ResolveForContactAsync(await LoadAsync(db, "a-sees-b"));
        await db.SaveChangesAsync();

        Assert.Null((await db.ContactAnnotations.SingleAsync(a => a.ContactItemId == "a-sees-b")).Cid);
    }

    [Fact]
    public async Task A_manual_link_is_never_touched()
    {
        using var db = NewContext();
        AddPeerContact(db, "a-sees-b", UserA, EmailB);
        db.ContactAnnotations.Add(new DbContactAnnotation
        {
            ContactItemId = "a-sees-b",
            Cid = CidBValue,
            WLId = EmailB,
            LinkIsManual = true,
        });
        await db.SaveChangesAsync();

        users.OnLookupUsersByEmail = _ => new LookupUsersByEmailResponse();
        await NewResolver(db).ResolveForContactAsync(await LoadAsync(db, "a-sees-b"));
        await db.SaveChangesAsync();

        var annotation = await db.ContactAnnotations.SingleAsync(a => a.ContactItemId == "a-sees-b");
        Assert.Equal(CidBValue, annotation.Cid);
        Assert.Equal(EmailB, annotation.WLId);
    }

    // the bluesky binding parks its own faux cid on the annotation, and that contact's address
    // belongs to no live user, so the resolver must not treat it as a stale link of its own
    [Fact]
    public async Task A_social_binding_cid_is_not_cleared()
    {
        const long FauxCid = 6021637387831981387;

        using var db = NewContext();
        AddPeerContact(db, "a-sees-amy", UserA, "amy@example.com");
        db.ContactAnnotations.Add(new DbContactAnnotation { ContactItemId = "a-sees-amy", Cid = FauxCid });
        db.ContactIdentities.Add(new DbContactIdentity
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = UserA,
            ContactItemId = "a-sees-amy",
            ContactCid = FauxCid,
            Provider = "atproto",
            ExternalId = "did:plc:ly7i5volax46sx4dnix3rbhs",
        });
        await db.SaveChangesAsync();

        users.OnLookupUsersByEmail = _ => new LookupUsersByEmailResponse();
        await NewResolver(db).ResolveForContactAsync(await LoadAsync(db, "a-sees-amy"));
        await db.SaveChangesAsync();

        Assert.Equal(FauxCid, (await db.ContactAnnotations.SingleAsync(a => a.ContactItemId == "a-sees-amy")).Cid);
    }

    [Fact]
    public async Task The_me_contact_is_never_linked()
    {
        using var db = NewContext();
        var me = await db.Items.OfType<DbContactItem>()
            .Include(c => c.Annotation)
            .SingleAsync(c => c.UserId == UserA && c.Annotation!.ContactType == "Me");

        users.OnLookupUsersByEmail = _ => throw new InvalidOperationException("should not have looked anything up");

        Assert.False(await NewResolver(db).ResolveForContactAsync(me));
        Assert.Null(me.Annotation!.Cid);
    }

    [Fact]
    public async Task A_contact_holding_the_owners_own_address_is_not_self_linked()
    {
        using var db = NewContext();
        AddPeerContact(db, "a-sees-self", UserA, EmailA);
        await db.SaveChangesAsync();

        users.OnLookupUsersByEmail = _ => Directory(UserA, "434e23def2c14ec2", EmailA, ProfileVisibility.Public);
        await NewResolver(db).ResolveForContactAsync(await LoadAsync(db, "a-sees-self"));
        await db.SaveChangesAsync();

        Assert.Null((await db.ContactAnnotations.SingleOrDefaultAsync(a => a.ContactItemId == "a-sees-self"))?.Cid);
    }

    [Fact]
    public async Task The_contact_itself_is_left_alone()
    {
        using var db = NewContext();
        AddPeerContact(db, "a-sees-b", UserA, EmailB, firstName: "bee", imAddress: "bee@elsewhere.test");
        await db.SaveChangesAsync();

        await ResolveAsync(db, "a-sees-b", ProfileVisibility.Public);

        var contact = await LoadAsync(db, "a-sees-b");
        Assert.Equal("bee", contact.FirstName);
        Assert.Equal("bee@elsewhere.test", contact.IMAddress);
        Assert.Null(contact.IMAddress2);
        Assert.Null(contact.Picture);
        Assert.Equal(EmailB, contact.Email1Address);
    }

    [Fact]
    public async Task Resolving_twice_emits_one_item_event()
    {
        using var db = NewContext();
        AddPeerContact(db, "a-sees-b", UserA, EmailB);
        await db.SaveChangesAsync();

        await ResolveAsync(db, "a-sees-b", ProfileVisibility.Public);
        var after = await db.ItemEvents.CountAsync(e => e.ServerId == "a-sees-b");

        await ResolveAsync(db, "a-sees-b", ProfileVisibility.Public);

        Assert.Equal(after, await db.ItemEvents.CountAsync(e => e.ServerId == "a-sees-b"));
    }

    [Fact]
    public async Task Linking_an_already_synced_contact_emits_an_item_event()
    {
        using var db = NewContext();
        AddPeerContact(db, "a-sees-b", UserA, EmailB);
        await db.SaveChangesAsync();

        var before = await db.ItemEvents.CountAsync(e => e.ServerId == "a-sees-b");
        await ResolveAsync(db, "a-sees-b", ProfileVisibility.Public);

        Assert.True(await db.ItemEvents.CountAsync(e => e.ServerId == "a-sees-b") > before);
    }

    private async Task ResolveAsync(MailboxDbContext db, string contactId, ProfileVisibility visibility)
    {
        users.OnLookupUsersByEmail = _ => Directory(UserB, CidB, EmailB, visibility);
        await NewResolver(db).ResolveForContactAsync(await LoadAsync(db, contactId));
        await db.SaveChangesAsync();
    }

    private static LookupUsersByEmailResponse Directory(string userId, string cid, string email, ProfileVisibility visibility) =>
        new()
        {
            Users =
            {
                new DirectoryUser
                {
                    QueriedEmail = ContactAddresses.Normalize(email)!,
                    UserId = userId,
                    Cid = cid,
                    EmailAddress = email,
                    Visibility = visibility,
                }
            }
        };

    private static Task<DbContactItem> LoadAsync(MailboxDbContext db, string contactId) =>
        db.Items.OfType<DbContactItem>().Include(c => c.Annotation).SingleAsync(c => c.Id == contactId);

    private ContactLinkResolver NewResolver(MailboxDbContext db) =>
        new(db, users,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["PublicUrls:Avatars"] = "https://relivewp.test/profile" })
                .Build(),
            NullLogger<ContactLinkResolver>.Instance);

    private static void AddFolder(MailboxDbContext db, string userId) =>
        db.Folders.Add(new DbFolder
        {
            Id = userId + "-contacts",
            UserId = userId,
            DisplayName = "Contacts",
            Type = DbFolderType.ContactsDefault,
        });

    private static void AddMeContact(MailboxDbContext db, string userId, string email)
    {
        var contact = new DbContactItem
        {
            Id = userId + "-me",
            ServerId = userId + "-me",
            UserId = userId,
            CollectionId = userId + "-contacts",
            Email1Address = email,
        };

        db.Items.Add(contact);
        db.ContactAnnotations.Add(new DbContactAnnotation
        {
            ContactItemId = contact.Id,
            ContactItem = contact,
            ContactType = "Me",
        });
    }

    private static void AddPeerContact(
        MailboxDbContext db, string id, string userId, string email,
        string? firstName = null, string? imAddress = null)
    {
        var contact = new DbContactItem
        {
            Id = id,
            ServerId = id,
            UserId = userId,
            CollectionId = userId + "-contacts",
            FirstName = firstName,
            Email1Address = email,
            IMAddress = imAddress,
        };

        db.Items.Add(contact);
        db.ContactEmails.Add(new DbContactEmail
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            ContactItemId = id,
            NormalizedAddress = ContactAddresses.Normalize(email)!,
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
}
