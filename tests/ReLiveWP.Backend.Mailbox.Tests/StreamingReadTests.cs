using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mailbox.Tests;

// the server-streaming reads: which rows come out, in what order, and that each item arrives with
// its children attached. nothing here cares whether the service buffers or streams underneath.
public class StreamingReadTests : MailboxStoreTestBase
{
    private const string ContactsFolderId = "contacts";
    private const string CalendarFolderId = "calendar";

    public StreamingReadTests()
    {
        SeedFolderAsync(ContactsFolderId, DbFolderType.ContactsDefault).GetAwaiter().GetResult();
        SeedFolderAsync(CalendarFolderId, DbFolderType.CalendarDefault).GetAwaiter().GetResult();
    }

    private Task<string> SeedContactAsync(string first, string[]? categories = null, DateTime? deletedAt = null,
        string? flaggedReason = null, DbContactAnnotation? annotation = null, string userId = UserId)
    {
        var id = NewId();
        return SeedItemAsync(new DbContactItem
        {
            Id = id,
            UserId = userId,
            CollectionId = ContactsFolderId,
            FirstName = first,
            FileAs = first,
            DeletedAt = deletedAt,
            ValidationFlaggedAt = flaggedReason is null ? null : DateTime.UtcNow,
            ValidationReason = flaggedReason,
            Categories = [.. (categories ?? []).Select(n => new DbContactCategory { Id = NewId(), ContactItemId = id, Name = n })],
            Annotation = annotation,
        });
    }

    private Task<string> SeedCalendarAsync(string subject, string[] attendees, bool flagged = false)
    {
        var id = NewId();
        return SeedItemAsync(new DbCalendarItem
        {
            Id = id,
            CollectionId = CalendarFolderId,
            Subject = subject,
            ValidationFlaggedAt = flagged ? DateTime.UtcNow : null,
            ValidationReason = flagged ? "test" : null,
            Attendees = [.. attendees.Select(a => new DbCalendarAttendee { Id = NewId(), CalendarItemId = id, Email = a, Name = a })],
        });
    }

    private async Task<List<T>> StreamAsync<T>(Func<MailboxStoreService, IServerStreamWriter<T>, ServerCallContext, Task> call)
    {
        await using var db = NewContext();
        var writer = new CollectingStreamWriter<T>();
        await call(NewService(db), writer, NewCallContext());
        return writer.Written;
    }

    [Fact]
    public async Task ListNetworks_orders_by_domain_then_email_and_scopes_to_the_user()
    {
        await using (var db = NewContext())
        {
            db.Networks.AddRange(
                new DbNetwork { Id = NewId(), UserId = UserId, DomainId = 2, UserEmail = "b@x" },
                new DbNetwork { Id = NewId(), UserId = UserId, DomainId = 1, UserEmail = "z@x" },
                new DbNetwork { Id = NewId(), UserId = UserId, DomainId = 1, UserEmail = "a@x" },
                new DbNetwork { Id = NewId(), UserId = OtherUserId, DomainId = 1, UserEmail = "theirs@x" });
            await db.SaveChangesAsync();
        }

        var networks = await StreamAsync<Network>((s, w, c) =>
            s.ListNetworks(new ListNetworksRequest { UserId = UserId }, w, c));

        Assert.Equal([(1, "a@x"), (1, "z@x"), (2, "b@x")], networks.Select(n => (n.DomainId, n.UserEmail)));
    }

    [Fact]
    public async Task ListItems_skips_deleted_and_flagged_items_and_attaches_children()
    {
        var live = await SeedContactAsync("Ada", categories: ["Friends", "Work"]);
        var deleted = await SeedContactAsync("Gone", deletedAt: DateTime.UtcNow);
        await SeedContactAsync("Bad", flaggedReason: "test");
        await SeedContactAsync("Theirs", userId: OtherUserId);

        var items = await StreamAsync<Item>((s, w, c) =>
            s.ListItems(new ListItemsRequest { UserId = UserId, CollectionId = ContactsFolderId }, w, c));

        var only = Assert.Single(items);
        Assert.Equal(live, only.ServerId);
        Assert.Equal(["Friends", "Work"], only.Contact.Categories.Select(x => x.Name).OrderBy(x => x));

        var withDeleted = await StreamAsync<Item>((s, w, c) =>
            s.ListItems(new ListItemsRequest { UserId = UserId, CollectionId = ContactsFolderId, IncludeDeleted = true }, w, c));

        Assert.Equal(new[] { deleted, live }.Order(), withDeleted.Select(i => i.ServerId).Order());
        Assert.NotNull(withDeleted.Single(i => i.ServerId == deleted).DeletedAt);
    }

    [Fact]
    public async Task ListItems_pages_through_a_collection_larger_than_one_chunk()
    {
        const int total = 450;
        await using (var db = NewContext())
        {
            for (var i = 0; i < total; i++)
            {
                var id = NewId();
                db.Items.Add(new DbContactItem
                {
                    Id = id,
                    ServerId = id,
                    UserId = UserId,
                    CollectionId = ContactsFolderId,
                    FirstName = $"c{i}",
                    FileAs = $"c{i}",
                    Categories = [new DbContactCategory { Id = NewId(), ContactItemId = id, Name = "Bulk" }],
                });
            }
            await db.SaveChangesAsync();
        }
        ResetSelectCount();

        var items = await StreamAsync<Item>((s, w, c) =>
            s.ListItems(new ListItemsRequest { UserId = UserId, CollectionId = ContactsFolderId }, w, c));

        Assert.Equal(total, items.Count);
        Assert.Equal(total, items.Select(i => i.ServerId).Distinct().Count());
        Assert.Equal(items.Select(i => i.ServerId).Order(), items.Select(i => i.ServerId));
        Assert.All(items, i => Assert.Equal(["Bulk"], i.Contact.Categories.Select(x => x.Name)));

        // three pages of 200, each one item query plus the three contact child tables
        Assert.Equal(12, SelectsExecuted);
    }

    [Fact]
    public async Task ListFlaggedItems_returns_only_live_flagged_items_with_their_reason()
    {
        await SeedContactAsync("Fine");
        var flagged = await SeedContactAsync("Bad", categories: ["Quarantine"], flaggedReason: "folder-class-congruence");
        await SeedContactAsync("BadAndGone", flaggedReason: "x", deletedAt: DateTime.UtcNow);
        await SeedContactAsync("TheirsBad", flaggedReason: "x", userId: OtherUserId);

        var items = await StreamAsync<FlaggedItem>((s, w, c) =>
            s.ListFlaggedItems(new ListFlaggedItemsRequest { UserId = UserId }, w, c));

        var only = Assert.Single(items);
        Assert.Equal(flagged, only.Item.ServerId);
        Assert.Equal("folder-class-congruence", only.Reason);
        Assert.NotNull(only.FlaggedAt);
        Assert.Equal(["Quarantine"], only.Item.Contact.Categories.Select(x => x.Name));
    }

    [Fact]
    public async Task GetItems_returns_the_requested_live_items_with_children_and_skips_flagged_ones()
    {
        var wanted = await SeedCalendarAsync("standup", ["a@x", "b@x"]);
        var flagged = await SeedCalendarAsync("bad", ["c@x"], flagged: true);
        var notAsked = await SeedCalendarAsync("other", []);

        var items = await StreamAsync<Item>((s, w, c) =>
            s.GetItems(new GetItemsRequest { UserId = UserId, ServerIds = { wanted, flagged, "no-such-item" } }, w, c));

        var only = Assert.Single(items);
        Assert.Equal(wanted, only.ServerId);
        Assert.Equal(["a@x", "b@x"], only.Calendar.Attendees.Select(a => a.Email).OrderBy(x => x));
        Assert.DoesNotContain(items, i => i.ServerId == notAsked);
    }

    [Fact]
    public async Task GetItems_never_crosses_users()
    {
        var theirs = await SeedContactAsync("Theirs", userId: OtherUserId);

        var items = await StreamAsync<Item>((s, w, c) =>
            s.GetItems(new GetItemsRequest { UserId = UserId, ServerIds = { theirs } }, w, c));

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetMeContact_returns_the_me_contact_with_its_annotation()
    {
        await SeedContactAsync("Ada");
        var me = await SeedContactAsync("Me", categories: ["Self"], annotation: new DbContactAnnotation { ContactType = "Me", Cid = 42 });

        await using var db = NewContext();
        var item = await NewService(db).GetMeContact(new GetMeContactRequest { UserId = UserId }, NewCallContext());

        Assert.Equal(me, item.ServerId);
        Assert.Equal("Me", item.Contact.Annotation.ContactType);
        Assert.Equal(42, item.Contact.Annotation.Cid);
        Assert.Equal(["Self"], item.Contact.Categories.Select(x => x.Name));
    }

    [Fact]
    public async Task GetMeContact_without_one_is_not_found()
    {
        await SeedContactAsync("Ada");

        await using var db = NewContext();
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            NewService(db).GetMeContact(new GetMeContactRequest { UserId = UserId }, NewCallContext()));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }
}
