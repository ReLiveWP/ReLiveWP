using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Services;
using ReLiveWP.Backend.Mailbox.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mailbox.Tests;

public class ContactIdentityBindingTests : IDisposable
{
    private const string User = "user-a";
    private const string Amy = "amy";
    private const long ProposedCid = 0x0123456789abcdef;
    private const long LiveCid = 0x15fe5d7a6d8d65ff;
    private const string Did = "did:plc:amyamyamyamyamyamyamy";

    private readonly SqliteConnection connection;

    public ContactIdentityBindingTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var db = NewContext();
        db.Database.EnsureCreated();
        db.Folders.Add(new DbFolder { Id = "contacts", UserId = User, DisplayName = "Contacts", Type = DbFolderType.ContactsDefault });
        db.Items.Add(new DbContactItem { Id = Amy, ServerId = Amy, UserId = User, CollectionId = "contacts", FirstName = "Amy" });
        db.SaveChanges();
    }

    public void Dispose() => connection.Dispose();

    [Fact]
    public async Task Binding_an_unlinked_contact_stamps_the_proposed_cid()
    {
        using var db = NewContext();

        var identity = await NewService(db).BindContactIdentity(Bind(), new StubCallContext());

        Assert.Equal(ProposedCid, identity.ContactCid);
        Assert.Equal(Amy, identity.ContactItemId);

        var annotation = await db.ContactAnnotations.SingleAsync(a => a.ContactItemId == Amy);
        Assert.Equal(ProposedCid, annotation.Cid);
    }

    [Fact]
    public async Task A_contact_that_already_has_a_cid_keeps_it()
    {
        using var db = NewContext();
        db.ContactAnnotations.Add(new DbContactAnnotation { ContactItemId = Amy, Cid = LiveCid, WLId = "amy@relivewp.net" });
        await db.SaveChangesAsync();

        var identity = await NewService(db).BindContactIdentity(Bind(), new StubCallContext());

        Assert.Equal(LiveCid, identity.ContactCid);
        Assert.Equal(LiveCid, (await db.ContactAnnotations.SingleAsync(a => a.ContactItemId == Amy)).Cid);
    }

    [Fact]
    public async Task Binding_writes_a_change_the_device_will_sync()
    {
        using var db = NewContext();
        var before = await db.ItemEvents.CountAsync(e => e.UserId == User);

        await NewService(db).BindContactIdentity(Bind(), new StubCallContext());

        Assert.Equal(before + 1, await db.ItemEvents.CountAsync(e => e.UserId == User));
    }

    [Fact]
    public async Task Unbinding_the_last_identity_clears_a_cid_it_created_but_not_a_live_one()
    {
        using var db = NewContext();
        var service = NewService(db);
        await service.BindContactIdentity(Bind(), new StubCallContext());

        var result = await service.UnbindContactIdentity(
            new UnbindContactIdentityRequest { UserId = User, ServerId = Amy, Provider = "atproto" }, new StubCallContext());

        Assert.True(result.Found);
        Assert.Empty(await db.ContactIdentities.ToListAsync());
        Assert.Null((await db.ContactAnnotations.SingleAsync(a => a.ContactItemId == Amy)).Cid);

        var annotation = await db.ContactAnnotations.SingleAsync(a => a.ContactItemId == Amy);
        annotation.Cid = LiveCid;
        annotation.WLId = "amy@relivewp.net";
        await db.SaveChangesAsync();
        await service.BindContactIdentity(Bind(), new StubCallContext());

        await service.UnbindContactIdentity(
            new UnbindContactIdentityRequest { UserId = User, ServerId = Amy, Provider = "atproto" }, new StubCallContext());

        Assert.Equal(LiveCid, (await db.ContactAnnotations.SingleAsync(a => a.ContactItemId == Amy)).Cid);
    }

    [Fact]
    public async Task Listing_returns_what_was_bound_and_a_missing_contact_is_not_found()
    {
        using var db = NewContext();
        var service = NewService(db);
        await service.BindContactIdentity(Bind(), new StubCallContext());

        var listed = await service.ListContactIdentities(
            new ListContactIdentitiesRequest { UserId = User, ServerId = Amy }, new StubCallContext());

        var identity = Assert.Single(listed.Identities);
        Assert.Equal(Did, identity.ExternalId);

        var missing = await Assert.ThrowsAsync<RpcException>(() => service.ListContactIdentities(
            new ListContactIdentitiesRequest { UserId = User, ServerId = "nobody" }, new StubCallContext()));
        Assert.Equal(StatusCode.NotFound, missing.StatusCode);
    }

    private static BindContactIdentityRequest Bind() => new()
    {
        UserId = User,
        ServerId = Amy,
        Provider = "atproto",
        ExternalId = Did,
        ContactCid = ProposedCid,
    };

    private static MailboxStoreService NewService(MailboxDbContext db) =>
        new(db, null!, null!, null!, new ContactLinkResolver(db, new FakeUserClient(), new ConfigurationBuilder().Build(), NullLogger<ContactLinkResolver>.Instance));

    private MailboxDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<MailboxDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new ChangeLogInterceptor())
            .Options;
        return new MailboxDbContext(options);
    }
}
