using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mailbox.Tests;

// UpdateItem replaces an item's child rows wholesale from the request. these pin the end state in
// the database, what GetItem hands back, and that the whole edit is one Update event, so the child
// sync can be reworked without a device ever seeing a difference.
public class UpdateItemChildSyncTests : MailboxStoreTestBase
{
    private const string ContactsFolderId = "contacts";
    private const string CalendarFolderId = "calendar";
    private const string NotesFolderId = "notes";

    public UpdateItemChildSyncTests()
    {
        SeedFolderAsync(ContactsFolderId, DbFolderType.ContactsDefault).GetAwaiter().GetResult();
        SeedFolderAsync(CalendarFolderId, DbFolderType.CalendarDefault).GetAwaiter().GetResult();
        SeedFolderAsync(NotesFolderId, DbFolderType.NotesDefault).GetAwaiter().GetResult();
    }

    private Task<string> SeedContactAsync(string[] categories, string[] children)
    {
        var id = NewId();
        return SeedItemAsync(new DbContactItem
        {
            Id = id,
            CollectionId = ContactsFolderId,
            FirstName = "Ada",
            FileAs = "Ada",
            Categories = [.. categories.Select(n => new DbContactCategory { Id = NewId(), ContactItemId = id, Name = n })],
            Children = [.. children.Select(n => new DbContactChild { Id = NewId(), ContactItemId = id, Name = n })],
        });
    }

    private Task<string> SeedCalendarAsync()
    {
        var id = NewId();
        var exceptionId = NewId();
        return SeedItemAsync(new DbCalendarItem
        {
            Id = id,
            CollectionId = CalendarFolderId,
            Subject = "standup",
            Attendees = [new DbCalendarAttendee { Id = NewId(), CalendarItemId = id, Email = "a1@x", Name = "a1" }],
            Categories = [new DbCalendarCategory { Id = NewId(), CalendarItemId = id, Category = "c1" }],
            Exceptions =
            [
                new DbCalendarException
                {
                    Id = exceptionId,
                    CalendarItemId = id,
                    ExceptionStartTime = "20260101T090000Z",
                    Attendees = [new DbCalendarExceptionAttendee { Id = NewId(), CalendarExceptionId = exceptionId, Email = "ea1@x", Name = "ea1" }],
                    Categories = [new DbCalendarExceptionCategory { Id = NewId(), CalendarExceptionId = exceptionId, Category = "ec1" }],
                },
            ],
        });
    }

    private Task<string> SeedNoteAsync(params string[] categories)
    {
        var id = NewId();
        return SeedItemAsync(new DbNote
        {
            Id = id,
            CollectionId = NotesFolderId,
            Subject = "note",
            Categories = [.. categories.Select(c => new DbNoteCategory { Id = NewId(), NoteItemId = id, Category = c })],
        });
    }

    private async Task<MutationResult> UpdateAsync(UpdateItemRequest request)
    {
        request.UserId = UserId;
        await using var db = NewContext();
        return await NewService(db).UpdateItem(request, NewCallContext());
    }

    private async Task<Item> GetAsync(string serverId)
    {
        await using var db = NewContext();
        return await NewService(db).GetItem(new GetItemRequest { UserId = UserId, ServerId = serverId }, NewCallContext());
    }

    private static void AssertSingleUpdateEvent(List<DbItemEvent> events)
    {
        Assert.Equal(2, events.Count);
        Assert.Equal(DbChangeEventType.Add, events[0].EventType);
        Assert.Equal(DbChangeEventType.Update, events[1].EventType);
    }

    [Fact]
    public async Task Contact_update_replaces_categories_and_children()
    {
        var serverId = await SeedContactAsync(["A", "B"], ["X"]);

        var result = await UpdateAsync(new UpdateItemRequest
        {
            ServerId = serverId,
            Contact = new ContactItem
            {
                FirstName = "Ada",
                FileAs = "Ada",
                Categories = { new ContactCategory { Name = "C" } },
                Children = { new ContactChild { Name = "Y" }, new ContactChild { Name = "Z" } },
            },
        });
        Assert.True(result.Found);

        await using var verify = NewContext();
        var categories = await verify.ContactCategories.Where(c => c.ContactItemId == serverId).Select(c => c.Name).ToListAsync();
        var children = await verify.ContactChildren.Where(c => c.ContactItemId == serverId).Select(c => c.Name).OrderBy(n => n).ToListAsync();
        Assert.Equal(["C"], categories);
        Assert.Equal(["Y", "Z"], children);

        var item = await GetAsync(serverId);
        Assert.Equal(["C"], item.Contact.Categories.Select(c => c.Name));
        Assert.Equal(["Y", "Z"], item.Contact.Children.Select(c => c.Name).OrderBy(n => n));

        AssertSingleUpdateEvent(await ItemEventsForAsync(serverId));
    }

    [Fact]
    public async Task Contact_update_with_no_children_clears_them()
    {
        var serverId = await SeedContactAsync(["A"], ["X"]);

        await UpdateAsync(new UpdateItemRequest
        {
            ServerId = serverId,
            Contact = new ContactItem { FirstName = "Ada", FileAs = "Ada" },
        });

        await using var verify = NewContext();
        Assert.Equal(0, await verify.ContactCategories.CountAsync(c => c.ContactItemId == serverId));
        Assert.Equal(0, await verify.ContactChildren.CountAsync(c => c.ContactItemId == serverId));

        var item = await GetAsync(serverId);
        Assert.Empty(item.Contact.Categories);
        Assert.Empty(item.Contact.Children);
    }

    [Fact]
    public async Task Contact_annotation_is_upserted_not_duplicated()
    {
        var serverId = await SeedContactAsync([], []);

        await UpdateAsync(new UpdateItemRequest
        {
            ServerId = serverId,
            Contact = new ContactItem { FirstName = "Ada", FileAs = "Ada" },
            Annotation = new ContactAnnotation { Cid = 5, ContactType = "Regular" },
        });
        await UpdateAsync(new UpdateItemRequest
        {
            ServerId = serverId,
            Contact = new ContactItem { FirstName = "Ada", FileAs = "Ada" },
            Annotation = new ContactAnnotation { Cid = 7, ContactType = "Regular" },
        });

        await using var verify = NewContext();
        var annotation = await verify.ContactAnnotations.SingleAsync(a => a.ContactItemId == serverId);
        Assert.Equal(7, annotation.Cid);

        var item = await GetAsync(serverId);
        Assert.Equal(7, item.Contact.Annotation.Cid);

        var events = await ItemEventsForAsync(serverId);
        Assert.Equal(3, events.Count);
        Assert.All(events.Skip(1), e => Assert.Equal(DbChangeEventType.Update, e.EventType));
    }

    [Fact]
    public async Task Calendar_update_replaces_every_child_table_including_exception_children()
    {
        var serverId = await SeedCalendarAsync();

        var result = await UpdateAsync(new UpdateItemRequest
        {
            ServerId = serverId,
            Calendar = new CalendarItem
            {
                Subject = "standup",
                Attendees =
                {
                    new CalendarAttendee { Email = "a2@x", Name = "a2" },
                    new CalendarAttendee { Email = "a3@x", Name = "a3", AttendeeStatus = 3 },
                },
                Exceptions =
                {
                    new CalendarException
                    {
                        ExceptionStartTime = "20260108T090000Z",
                        Subject = "moved",
                        Attendees = { new CalendarExceptionAttendee { Email = "ea2@x", Name = "ea2" } },
                        Categories =
                        {
                            new CalendarExceptionCategory { Category = "ec2" },
                            new CalendarExceptionCategory { Category = "ec3" },
                        },
                    },
                },
            },
        });
        Assert.True(result.Found);

        await using var verify = NewContext();
        var attendees = await verify.CalendarAttendees.Where(a => a.CalendarItemId == serverId).OrderBy(a => a.Email).ToListAsync();
        Assert.Equal(["a2@x", "a3@x"], attendees.Select(a => a.Email));
        Assert.Equal((byte)3, attendees[1].AttendeeStatus);
        Assert.Equal(0, await verify.CalendarCategories.CountAsync(c => c.CalendarItemId == serverId));

        var exception = await verify.CalendarExceptions.SingleAsync(e => e.CalendarItemId == serverId);
        Assert.Equal("20260108T090000Z", exception.ExceptionStartTime);
        Assert.Equal("moved", exception.Subject);

        var exceptionAttendees = await verify.CalendarExceptionAttendees.ToListAsync();
        var exceptionCategories = await verify.CalendarExceptionCategories.OrderBy(c => c.Category).ToListAsync();
        Assert.Equal(["ea2@x"], exceptionAttendees.Select(a => a.Email));
        Assert.All(exceptionAttendees, a => Assert.Equal(exception.Id, a.CalendarExceptionId));
        Assert.Equal(["ec2", "ec3"], exceptionCategories.Select(c => c.Category));
        Assert.All(exceptionCategories, c => Assert.Equal(exception.Id, c.CalendarExceptionId));

        var item = await GetAsync(serverId);
        Assert.Equal(["a2@x", "a3@x"], item.Calendar.Attendees.Select(a => a.Email).OrderBy(e => e));
        Assert.Empty(item.Calendar.Categories);
        var protoException = Assert.Single(item.Calendar.Exceptions);
        Assert.Equal(["ea2@x"], protoException.Attendees.Select(a => a.Email));
        Assert.Equal(["ec2", "ec3"], protoException.Categories.Select(c => c.Category).OrderBy(c => c));

        AssertSingleUpdateEvent(await ItemEventsForAsync(serverId));
    }

    [Fact]
    public async Task Calendar_update_with_no_exceptions_clears_exceptions_and_their_children()
    {
        var serverId = await SeedCalendarAsync();

        await UpdateAsync(new UpdateItemRequest
        {
            ServerId = serverId,
            Calendar = new CalendarItem { Subject = "standup" },
        });

        await using var verify = NewContext();
        Assert.Equal(0, await verify.CalendarAttendees.CountAsync(a => a.CalendarItemId == serverId));
        Assert.Equal(0, await verify.CalendarCategories.CountAsync(c => c.CalendarItemId == serverId));
        Assert.Equal(0, await verify.CalendarExceptions.CountAsync(e => e.CalendarItemId == serverId));
        Assert.Equal(0, await verify.CalendarExceptionAttendees.CountAsync());
        Assert.Equal(0, await verify.CalendarExceptionCategories.CountAsync());
    }

    [Fact]
    public async Task Calendar_update_rejected_by_validation_leaves_the_old_children_in_place()
    {
        var serverId = await SeedCalendarAsync();

        var ex = await Assert.ThrowsAsync<RpcException>(() => UpdateAsync(new UpdateItemRequest
        {
            ServerId = serverId,
            Calendar = new CalendarItem
            {
                Subject = "standup",
                Attendees = { new CalendarAttendee { Email = "a2@x", Name = "a2" } },
                Exceptions = { new CalendarException { Subject = "no start time" } },
            },
        }));
        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);

        var item = await GetAsync(serverId);
        Assert.Equal(["a1@x"], item.Calendar.Attendees.Select(a => a.Email));
        Assert.Equal(["c1"], item.Calendar.Categories.Select(c => c.Category));
        var exception = Assert.Single(item.Calendar.Exceptions);
        Assert.Equal(["ea1@x"], exception.Attendees.Select(a => a.Email));
        Assert.Equal(["ec1"], exception.Categories.Select(c => c.Category));

        var events = await ItemEventsForAsync(serverId);
        Assert.Single(events);
    }

    [Fact]
    public async Task Calendar_update_reads_the_item_and_each_child_table_once()
    {
        var serverId = await SeedCalendarAsync();
        ResetSelectCount();

        await UpdateAsync(new UpdateItemRequest
        {
            ServerId = serverId,
            Calendar = new CalendarItem
            {
                Subject = "renamed",
                Attendees = { new CalendarAttendee { Email = "a2@x", Name = "a2" } },
                Exceptions = { new CalendarException { ExceptionStartTime = "20260108T090000Z" } },
            },
        });

        // item, attendees, categories, exceptions, exception attendees, exception categories, then
        // the folder type the validation interceptor resolves
        Assert.Equal(7, SelectsExecuted);
    }

    [Fact]
    public async Task Note_update_reads_the_item_and_its_categories_once()
    {
        var serverId = await SeedNoteAsync("n1");
        ResetSelectCount();

        await UpdateAsync(new UpdateItemRequest
        {
            ServerId = serverId,
            Note = new NoteItem { Subject = "renamed", Categories = { new NoteCategory { Category = "n2" } } },
        });

        // item, categories, folder type
        Assert.Equal(3, SelectsExecuted);
    }

    [Fact]
    public async Task Note_update_replaces_categories()
    {
        var serverId = await SeedNoteAsync("n1", "n2");

        await UpdateAsync(new UpdateItemRequest
        {
            ServerId = serverId,
            Note = new NoteItem { Subject = "note", Categories = { new NoteCategory { Category = "n3" } } },
        });

        await using var verify = NewContext();
        var categories = await verify.NoteCategories.Where(c => c.NoteItemId == serverId).Select(c => c.Category).ToListAsync();
        Assert.Equal(["n3"], categories);

        var item = await GetAsync(serverId);
        Assert.Equal(["n3"], item.Note.Categories.Select(c => c.Category));

        AssertSingleUpdateEvent(await ItemEventsForAsync(serverId));
    }

    [Fact]
    public async Task Update_of_a_soft_deleted_item_is_not_found_and_touches_nothing()
    {
        var serverId = await SeedContactAsync(["A"], []);
        await using (var db = NewContext())
        {
            var contact = await db.Items.SingleAsync(i => i.ServerId == serverId);
            contact.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var result = await UpdateAsync(new UpdateItemRequest
        {
            ServerId = serverId,
            Contact = new ContactItem { FirstName = "Ada", Categories = { new ContactCategory { Name = "C" } } },
        });

        Assert.False(result.Found);
        await using var verify = NewContext();
        Assert.Equal(["A"], await verify.ContactCategories.Where(c => c.ContactItemId == serverId).Select(c => c.Name).ToListAsync());
    }
}
