using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mailbox.Tests;

// a device Add carries the whole item, children included. CreateItem used to keep only the scalar
// half and the attendees never arrived until the item was edited.
public class CreateItemChildTests : MailboxStoreTestBase
{
    private const string ContactsFolderId = "contacts";
    private const string CalendarFolderId = "calendar";
    private const string NotesFolderId = "notes";

    public CreateItemChildTests()
    {
        SeedFolderAsync(ContactsFolderId, DbFolderType.ContactsDefault).GetAwaiter().GetResult();
        SeedFolderAsync(CalendarFolderId, DbFolderType.CalendarDefault).GetAwaiter().GetResult();
        SeedFolderAsync(NotesFolderId, DbFolderType.NotesDefault).GetAwaiter().GetResult();
    }

    private async Task<Item> CreateAsync(CreateItemRequest request)
    {
        request.UserId = UserId;
        await using var db = NewContext();
        return await NewService(db).CreateItem(request, NewCallContext());
    }

    private async Task<Item> GetAsync(string serverId)
    {
        await using var db = NewContext();
        return await NewService(db).GetItem(new GetItemRequest { UserId = UserId, ServerId = serverId }, NewCallContext());
    }

    [Fact]
    public async Task Contact_create_persists_categories_children_and_annotation()
    {
        var created = await CreateAsync(new CreateItemRequest
        {
            CollectionId = ContactsFolderId,
            Contact = new ContactItem
            {
                FirstName = "Ada",
                FileAs = "Ada",
                Categories = { new ContactCategory { Name = "Friends" }, new ContactCategory { Name = "Work" } },
                Children = { new ContactChild { Name = "Byron" } },
            },
            Annotation = new ContactAnnotation { ContactType = "Regular" },
        });

        var item = await GetAsync(created.ServerId);
        Assert.Equal(["Friends", "Work"], item.Contact.Categories.Select(c => c.Name).Order());
        Assert.Equal(["Byron"], item.Contact.Children.Select(c => c.Name));
        Assert.Equal("Regular", item.Contact.Annotation.ContactType);

        await using var verify = NewContext();
        Assert.Equal(2, await verify.ContactCategories.CountAsync(c => c.ContactItemId == created.ServerId));
        Assert.Equal(1, await verify.ContactChildren.CountAsync(c => c.ContactItemId == created.ServerId));
        Assert.Single(await ItemEventsForAsync(created.ServerId));
    }

    [Fact]
    public async Task Calendar_create_persists_attendees_categories_and_exceptions_with_their_children()
    {
        var created = await CreateAsync(new CreateItemRequest
        {
            CollectionId = CalendarFolderId,
            Calendar = new CalendarItem
            {
                Subject = "standup",
                Attendees = { new CalendarAttendee { Email = "a@x", Name = "a", AttendeeStatus = 3 } },
                Categories = { new CalendarCategory { Category = "Work" } },
                Exceptions =
                {
                    new CalendarException
                    {
                        ExceptionStartTime = "20260108T090000Z",
                        Attendees = { new CalendarExceptionAttendee { Email = "ea@x", Name = "ea" } },
                        Categories = { new CalendarExceptionCategory { Category = "Moved" } },
                    },
                },
            },
        });

        var item = await GetAsync(created.ServerId);
        var attendee = Assert.Single(item.Calendar.Attendees);
        Assert.Equal(("a@x", 3u), (attendee.Email, attendee.AttendeeStatus));
        Assert.Equal(["Work"], item.Calendar.Categories.Select(c => c.Category));
        var exception = Assert.Single(item.Calendar.Exceptions);
        Assert.Equal("20260108T090000Z", exception.ExceptionStartTime);
        Assert.Equal(["ea@x"], exception.Attendees.Select(a => a.Email));
        Assert.Equal(["Moved"], exception.Categories.Select(c => c.Category));

        Assert.Single(await ItemEventsForAsync(created.ServerId));
    }

    [Fact]
    public async Task Calendar_create_with_an_invalid_exception_is_rejected_whole()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(() => CreateAsync(new CreateItemRequest
        {
            CollectionId = CalendarFolderId,
            Calendar = new CalendarItem
            {
                Subject = "standup",
                Exceptions = { new CalendarException { Subject = "no start time" } },
            },
        }));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        await using var verify = NewContext();
        Assert.Equal(0, await verify.Items.CountAsync(i => i.UserId == UserId));
        Assert.Equal(0, await verify.CalendarExceptions.CountAsync());
    }

    [Fact]
    public async Task Note_create_persists_categories()
    {
        var created = await CreateAsync(new CreateItemRequest
        {
            CollectionId = NotesFolderId,
            Note = new NoteItem { Subject = "note", Categories = { new NoteCategory { Category = "Ideas" } } },
        });

        var item = await GetAsync(created.ServerId);
        Assert.Equal(["Ideas"], item.Note.Categories.Select(c => c.Category));
    }
}
