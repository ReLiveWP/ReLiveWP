using Grpc.Core;
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

// messenger presence follows the same discovery rules as contact linking: you see a linked contact
// when they're discoverable to you, and they're watched by whoever they're discoverable to
public class ListPresenceAudienceTests : IDisposable
{
    private const string UserA = "user-a";
    private const string UserB = "user-b";
    private const string EmailA = "ada@relivewp.net";
    private const string EmailB = "wam@relivewp.net";
    private const string CidA = "0000000000000a0a";
    private const string CidB = "15fe5d7a6d8d65ff";

    private readonly SqliteConnection connection;
    private readonly FakeUserClient users = new();
    private readonly Dictionary<string, ProfileVisibility> visibility = new()
    {
        [UserA] = ProfileVisibility.Private,
        [UserB] = ProfileVisibility.Private,
    };

    private static readonly Dictionary<string, (string UserId, string Cid)> Accounts = new(StringComparer.OrdinalIgnoreCase)
    {
        [EmailA] = (UserA, CidA),
        [EmailB] = (UserB, CidB),
    };

    public ListPresenceAudienceTests()
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

        users.OnLookupUsersByEmail = LookupDirectory;
        users.OnGetUserProfile = request =>
        {
            var (email, (userId, cid)) = Accounts.Single(a => a.Value.UserId == request.UserId);
            return new UserProfile { UserId = userId, Cid = cid, EmailAddress = email, Visibility = visibility[userId] };
        };
    }

    public void Dispose() => connection.Dispose();

    [Fact]
    public async Task A_public_contact_is_seen_one_way()
    {
        visibility[UserB] = ProfileVisibility.Public;
        using var db = NewContext();
        AddLinkedContact(db, "a-has-b", UserA, EmailB, CidB);
        await db.SaveChangesAsync();

        var a = await AudienceAsync(db, UserA);
        var b = await AudienceAsync(db, UserB);

        Assert.Equal([UserB], a.VisibleUserIds);
        Assert.Empty(a.WatcherUserIds);
        Assert.Empty(b.VisibleUserIds);
        Assert.Equal([UserA], b.WatcherUserIds);
    }

    [Fact]
    public async Task A_mutual_contact_needs_you_in_their_book_too()
    {
        visibility[UserB] = ProfileVisibility.Mutual;
        using var db = NewContext();
        AddLinkedContact(db, "a-has-b", UserA, EmailB, CidB);
        await db.SaveChangesAsync();

        Assert.Empty((await AudienceAsync(db, UserA)).VisibleUserIds);
        Assert.Empty((await AudienceAsync(db, UserB)).WatcherUserIds);

        AddLinkedContact(db, "b-has-a", UserB, EmailA, CidA);
        await db.SaveChangesAsync();

        Assert.Equal([UserB], (await AudienceAsync(db, UserA)).VisibleUserIds);
        Assert.Equal([UserA], (await AudienceAsync(db, UserB)).WatcherUserIds);
    }

    [Fact]
    public async Task A_private_contact_is_never_seen()
    {
        using var db = NewContext();
        AddLinkedContact(db, "a-has-b", UserA, EmailB, CidB);
        AddLinkedContact(db, "b-has-a", UserB, EmailA, CidA);
        await db.SaveChangesAsync();

        Assert.Empty((await AudienceAsync(db, UserA)).VisibleUserIds);
        Assert.Empty((await AudienceAsync(db, UserB)).WatcherUserIds);
    }

    [Fact]
    public async Task A_deleted_contact_drops_out()
    {
        visibility[UserB] = ProfileVisibility.Public;
        using var db = NewContext();
        var contact = AddLinkedContact(db, "a-has-b", UserA, EmailB, CidB);
        contact.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        Assert.Empty((await AudienceAsync(db, UserA)).VisibleUserIds);
        Assert.Empty((await AudienceAsync(db, UserB)).WatcherUserIds);
    }

    [Fact]
    public async Task A_directory_failure_is_an_error_not_an_empty_audience()
    {
        visibility[UserB] = ProfileVisibility.Public;
        using var db = NewContext();
        AddLinkedContact(db, "a-has-b", UserA, EmailB, CidB);
        await db.SaveChangesAsync();
        users.OnLookupUsersByEmail = _ => throw new RpcException(new Status(StatusCode.Unavailable, "identity is down"));

        var error = await Assert.ThrowsAsync<RpcException>(() => AudienceAsync(db, UserA));

        Assert.Equal(StatusCode.Unavailable, error.StatusCode);
    }

    private LookupUsersByEmailResponse LookupDirectory(LookupUsersByEmailRequest request)
    {
        var response = new LookupUsersByEmailResponse();
        foreach (var email in request.Emails)
        {
            if (!Accounts.TryGetValue(email, out var account) || visibility[account.UserId] == ProfileVisibility.Private)
                continue;

            response.Users.Add(new DirectoryUser
            {
                QueriedEmail = email,
                UserId = account.UserId,
                Cid = account.Cid,
                EmailAddress = email,
                Visibility = visibility[account.UserId],
            });
        }

        return response;
    }

    private Task<ListPresenceAudienceResponse> AudienceAsync(MailboxDbContext db, string userId)
    {
        var resolver = new ContactLinkResolver(db, users, new ConfigurationBuilder().Build(), NullLogger<ContactLinkResolver>.Instance);
        return new MailboxStoreService(db, null!, null!, null!, resolver)
            .ListPresenceAudience(new ListPresenceAudienceRequest { UserId = userId }, new StubCallContext());
    }

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

    private static DbContactItem AddLinkedContact(MailboxDbContext db, string id, string ownerId, string email, string cid)
    {
        var contact = new DbContactItem
        {
            Id = id,
            ServerId = id,
            UserId = ownerId,
            CollectionId = ownerId + "-contacts",
            Email1Address = email,
        };

        db.Items.Add(contact);
        db.ContactEmails.Add(new DbContactEmail
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = ownerId,
            ContactItemId = id,
            NormalizedAddress = ContactAddresses.Normalize(email)!,
        });
        db.ContactAnnotations.Add(new DbContactAnnotation
        {
            ContactItemId = contact.Id,
            ContactItem = contact,
            Cid = Convert.ToInt64(cid, 16),
            WLId = email,
            ImMri = "1:" + email,
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
