using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Tests;

public class MeContactWritebackTests
{
    private const string User = "user-1";
    private const string ServerId = "me-contact";
    private const string AccountEmail = "wam@relivewp.net";

    private static ContactItem Contact(string? contactType, string firstName = "Thomas", string? lastName = "May")
    {
        var contact = new ContactItem
        {
            FirstName = firstName,
            Email1Address = AccountEmail,
            ImAddress = AccountEmail,
            ImAddress2 = "1:" + AccountEmail,
        };

        if (lastName is not null) contact.LastName = lastName;
        if (contactType is not null) contact.Annotation = new ContactAnnotation { ContactType = contactType };

        return contact;
    }

    private static Item Item(ContactItem contact) => new()
    {
        Id = ServerId,
        UserId = User,
        ServerId = ServerId,
        CollectionId = "c1",
        Version = 1,
        Contact = contact,
    };

    private static SyncChange Change(string name, string value)
    {
        var doc = new System.Xml.XmlDocument();
        var el = doc.CreateElement(name, Constants.Contacts);
        el.InnerText = value;

        return new SyncChange
        {
            ServerId = ServerId,
            ApplicationData = new ApplicationData { Elements = { el } },
        };
    }

    private static (ItemSyncService Service, FakeUserClient Users, List<UpdateItemRequest> Writes) NewService(ContactItem existing)
    {
        var writes = new List<UpdateItemRequest>();
        var mailbox = new FakeMailboxStoreClient
        {
            OnGetItem = _ => Item(existing),
            OnUpdateItem = req => { writes.Add(req); return new MutationResult { Found = true }; },
        };

        var users = new FakeUserClient();
        var writeback = new MeProfileWriteback(users, NullLogger<MeProfileWriteback>.Instance);

        var service = new ItemSyncService(mailbox, NullLogger<ItemSyncService>.Instance,
            Options.Create(new EasSyncOptions()), writeback);

        return (service, users, writes);
    }

    private static Task<int> Apply(ItemSyncService service, SyncChange change) =>
        service.HandleChangeAsync(User, "Contact", change, new HashSet<string>(),
            SyncConflict.ClientWins, GhostingPolicy.GhostAll, CancellationToken.None);

    [Fact]
    public async Task Name_change_on_the_me_contact_is_written_back()
    {
        var (service, users, _) = NewService(Contact("Me"));

        var status = await Apply(service, Change("FirstName", "Ada"));

        Assert.Equal(1, status);
        var update = Assert.Single(users.ProfileUpdates);
        Assert.Equal("Ada", update.FirstName);
    }

    [Fact]
    public async Task Name_change_on_an_ordinary_contact_is_not_written_back()
    {
        var (service, users, _) = NewService(Contact(contactType: null));

        await Apply(service, Change("FirstName", "Ada"));

        Assert.Empty(users.ProfileUpdates);
    }

    [Fact]
    public async Task An_unchanged_name_is_not_written_back()
    {
        var (service, users, _) = NewService(Contact("Me"));

        await Apply(service, Change("FirstName", "Thomas"));

        Assert.Empty(users.ProfileUpdates);
    }

    [Fact]
    public async Task A_new_picture_is_written_back()
    {
        var (service, users, _) = NewService(Contact("Me"));
        var picture = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });

        await Apply(service, Change("Picture", picture));

        var set = Assert.Single(users.PictureSets);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, set.Data.ToByteArray());
    }

    [Fact]
    public async Task An_unchanged_picture_is_not_written_back()
    {
        var existing = Contact("Me");
        existing.Picture = ByteString.CopyFrom([1, 2, 3, 4]);

        var (service, users, _) = NewService(existing);
        var picture = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });

        await Apply(service, Change("Picture", picture));

        Assert.Empty(users.PictureSets);
    }

    [Fact]
    public async Task An_emptied_picture_clears_the_account_picture()
    {
        var existing = Contact("Me");
        existing.Picture = ByteString.CopyFrom([1, 2, 3, 4]);

        var (service, users, _) = NewService(existing);

        await Apply(service, Change("Picture", ""));

        Assert.Single(users.PictureClears);
    }

    // the account owns the addresses: a device edit must not reach the store or the account
    [Fact]
    public async Task Email_edits_on_the_me_contact_are_discarded()
    {
        var (service, users, writes) = NewService(Contact("Me"));

        await Apply(service, Change("Email1Address", "someone@else.example"));

        var write = Assert.Single(writes);
        Assert.Equal(AccountEmail, write.Contact.Email1Address);
        Assert.Equal(AccountEmail, write.Contact.ImAddress);
        Assert.Equal("1:" + AccountEmail, write.Contact.ImAddress2);
        Assert.Empty(users.ProfileUpdates);
    }

    [Fact]
    public async Task Email_edits_on_an_ordinary_contact_still_apply()
    {
        var (service, _, writes) = NewService(Contact(contactType: null));

        await Apply(service, Change("Email1Address", "someone@else.example"));

        var write = Assert.Single(writes);
        Assert.Equal("someone@else.example", write.Contact.Email1Address);
    }
}
