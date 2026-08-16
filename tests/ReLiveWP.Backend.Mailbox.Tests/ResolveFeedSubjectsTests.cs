using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Services;
using ReLiveWP.Backend.Mailbox.Services.Grpc;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mailbox.Tests;

// whether a viewer may read a cid's feed is decided here, so these are the assertions that stop a
// stale annotation from outliving the permission that created it
public class ResolveFeedSubjectsTests : IDisposable
{
    private const string UserA = "user-a";
    private const string UserB = "user-b";
    private const string UserC = "user-c";
    private const string EmailA = "ada@relivewp.net";
    private const string EmailB = "wam@relivewp.net";
    private const string CidB = "15fe5d7a6d8d65ff";
    private const long CidBValue = 0x15fe5d7a6d8d65ff;

    private readonly SqliteConnection connection;
    private readonly FakeUserClient users = new();

    public ResolveFeedSubjectsTests()
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
    public async Task A_public_target_resolves_to_its_account()
    {
        using var db = NewContext();
        AddLinkedContact(db, "a-sees-b", UserA, EmailB, CidBValue);
        await db.SaveChangesAsync();

        var subject = await ResolveAsync(db, UserA, CidBValue, ProfileVisibility.Public);

        Assert.Equal(FeedSubjectKind.LiveUser, subject.Kind);
        Assert.Equal(UserB, subject.SubjectUserId);
    }

    // the annotation survives reciprocity being withdrawn, the permission must not
    [Fact]
    public async Task A_mutual_target_stops_resolving_when_reciprocity_goes_away()
    {
        using var db = NewContext();
        AddLinkedContact(db, "a-sees-b", UserA, EmailB, CidBValue);
        AddPeerContact(db, "b-sees-a", UserB, EmailA);
        await db.SaveChangesAsync();

        Assert.Equal(FeedSubjectKind.LiveUser, (await ResolveAsync(db, UserA, CidBValue, ProfileVisibility.Mutual)).Kind);

        db.ContactEmails.RemoveRange(db.ContactEmails.Where(e => e.ContactItemId == "b-sees-a"));
        db.Items.RemoveRange(db.Items.Where(i => i.Id == "b-sees-a"));
        await db.SaveChangesAsync();

        var after = await ResolveAsync(db, UserA, CidBValue, ProfileVisibility.Mutual);

        Assert.Equal(FeedSubjectKind.Unknown, after.Kind);
        Assert.Equal(CidBValue, await db.ContactAnnotations.Where(a => a.ContactItemId == "a-sees-b").Select(a => a.Cid!.Value).SingleAsync());
    }

    [Fact]
    public async Task A_private_target_does_not_resolve()
    {
        using var db = NewContext();
        AddLinkedContact(db, "a-sees-b", UserA, EmailB, CidBValue);
        await db.SaveChangesAsync();

        Assert.Equal(FeedSubjectKind.Unknown, (await ResolveAsync(db, UserA, CidBValue, ProfileVisibility.Private)).Kind);
    }

    // a manual link does not bypass discovery, but it does not forfeit it either
    [Fact]
    public async Task A_manual_link_resolves_for_a_public_target_and_not_a_private_one()
    {
        using var db = NewContext();
        AddLinkedContact(db, "a-sees-b", UserA, EmailB, CidBValue, manual: true);
        await db.SaveChangesAsync();

        Assert.Equal(FeedSubjectKind.LiveUser, (await ResolveAsync(db, UserA, CidBValue, ProfileVisibility.Public)).Kind);
        Assert.Equal(FeedSubjectKind.Unknown, (await ResolveAsync(db, UserA, CidBValue, ProfileVisibility.Private)).Kind);
    }

    [Fact]
    public async Task Another_users_contact_does_not_resolve()
    {
        using var db = NewContext();
        AddLinkedContact(db, "a-sees-b", UserA, EmailB, CidBValue);
        await db.SaveChangesAsync();

        Assert.Equal(FeedSubjectKind.Unknown, (await ResolveAsync(db, UserC, CidBValue, ProfileVisibility.Public)).Kind);
    }

    [Fact]
    public async Task A_deleted_contact_does_not_resolve()
    {
        using var db = NewContext();
        AddLinkedContact(db, "a-sees-b", UserA, EmailB, CidBValue);
        await db.SaveChangesAsync();

        var contact = await db.Items.OfType<DbContactItem>().SingleAsync(c => c.Id == "a-sees-b");
        contact.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        Assert.Equal(FeedSubjectKind.Unknown, (await ResolveAsync(db, UserA, CidBValue, ProfileVisibility.Public)).Kind);
    }

    // the address is what gets looked up, so it has to still name the account the cid claims
    [Fact]
    public async Task An_address_pointing_at_a_different_account_does_not_resolve()
    {
        using var db = NewContext();
        AddLinkedContact(db, "a-sees-b", UserA, EmailB, CidBValue);
        await db.SaveChangesAsync();

        users.OnLookupUsersByEmail = _ => Directory(UserB, "0badc0de0badc0de", EmailB, ProfileVisibility.Public);

        Assert.Equal(FeedSubjectKind.Unknown, (await ResolveRawAsync(db, UserA, CidBValue)).Kind);
    }

    [Fact]
    public async Task A_faux_identity_resolves_to_its_provider_and_external_id()
    {
        const long FauxCid = 6021637387831981387;

        using var db = NewContext();
        AddPeerContact(db, "a-sees-amy", UserA, "amy@example.com");
        db.ContactIdentities.Add(new DbContactIdentity
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = UserA,
            ContactItemId = "a-sees-amy",
            ContactCid = FauxCid,
            Provider = "atproto",
            ExternalId = "did:plc:amyamyamyamyamyamyamy",
        });
        await db.SaveChangesAsync();

        users.OnLookupUsersByEmail = _ => new LookupUsersByEmailResponse();
        var subject = await ResolveRawAsync(db, UserA, FauxCid);

        Assert.Equal(FeedSubjectKind.ContactIdentity, subject.Kind);
        var identity = Assert.Single(subject.Identities);
        Assert.Equal("atproto", identity.Provider);
        Assert.Equal("did:plc:amyamyamyamyamyamyamy", identity.ExternalId);
    }

    [Fact]
    public async Task An_unknown_cid_resolves_to_nothing()
    {
        using var db = NewContext();
        users.OnLookupUsersByEmail = _ => new LookupUsersByEmailResponse();

        Assert.Equal(FeedSubjectKind.Unknown, (await ResolveRawAsync(db, UserA, 1234)).Kind);
    }

    [Fact]
    public async Task A_batch_of_cids_costs_one_directory_call()
    {
        using var db = NewContext();
        AddLinkedContact(db, "a-sees-b", UserA, EmailB, CidBValue);
        AddLinkedContact(db, "a-sees-c", UserA, "cee@relivewp.net", 0x0123456789abcdef);
        await db.SaveChangesAsync();

        users.OnLookupUsersByEmail = _ => Directory(UserB, CidB, EmailB, ProfileVisibility.Public);

        var before = users.LookupUsersByEmailCalls;
        var response = await NewService(db).ResolveFeedSubjects(new ResolveFeedSubjectsRequest
        {
            UserId = UserA,
            Cids = { CidBValue, 0x0123456789abcdef },
        }, new StubCallContext());

        Assert.Equal(2, response.Subjects.Count);
        Assert.Equal(before + 1, users.LookupUsersByEmailCalls);
    }

    private async Task<FeedSubject> ResolveAsync(MailboxDbContext db, string viewerUserId, long cid, ProfileVisibility visibility)
    {
        users.OnLookupUsersByEmail = _ => Directory(UserB, CidB, EmailB, visibility);
        return await ResolveRawAsync(db, viewerUserId, cid);
    }

    private async Task<FeedSubject> ResolveRawAsync(MailboxDbContext db, string viewerUserId, long cid)
    {
        var response = await NewService(db).ResolveFeedSubjects(
            new ResolveFeedSubjectsRequest { UserId = viewerUserId, Cids = { cid } }, new StubCallContext());

        return response.Subjects.Single();
    }

    private static LookupUsersByEmailResponse Directory(string userId, string cid, string email, ProfileVisibility visibility)
    {
        // private users never come back from the real lookup, the resolver must not need telling twice
        if (visibility == ProfileVisibility.Private)
            return new LookupUsersByEmailResponse();

        return new LookupUsersByEmailResponse
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
    }

    private MailboxStoreService NewService(MailboxDbContext db) =>
        new(db, null!, null!, null!, NewResolver(db));

    private ContactLinkResolver NewResolver(MailboxDbContext db) =>
        new(db, users, new ConfigurationBuilder().Build(), NullLogger<ContactLinkResolver>.Instance);

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

    private static DbContactItem AddPeerContact(MailboxDbContext db, string id, string userId, string email)
    {
        var contact = new DbContactItem
        {
            Id = id,
            ServerId = id,
            UserId = userId,
            CollectionId = userId + "-contacts",
            Email1Address = email,
        };

        db.Items.Add(contact);
        db.ContactEmails.Add(new DbContactEmail
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            ContactItemId = id,
            NormalizedAddress = ContactAddresses.Normalize(email)!,
        });

        return contact;
    }

    private static void AddLinkedContact(
        MailboxDbContext db, string id, string userId, string email, long cid, bool manual = false)
    {
        var contact = AddPeerContact(db, id, userId, email);

        db.ContactAnnotations.Add(new DbContactAnnotation
        {
            ContactItemId = contact.Id,
            ContactItem = contact,
            Cid = cid,
            WLId = email,
            ImMri = "1:" + email,
            LinkIsManual = manual,
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
