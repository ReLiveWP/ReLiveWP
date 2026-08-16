using Google.Protobuf.WellKnownTypes;
using ReLiveWP.Services.Exchange.Extensions;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Tests;

public class CollectionRoundTripTests
{
    private static string Xml(ApplicationData appData) =>
        string.Concat(appData.Elements.Select(e => e.OuterXml));

    private static Timestamp Ts(int y, int mo, int d, int h, int mi) =>
        Timestamp.FromDateTime(new DateTime(y, mo, d, h, mi, 0, DateTimeKind.Utc));

    // --- outbound: server -> device -------------------------------------------------

    [Fact]
    public void Contact_categories_and_children_are_serialized()
    {
        var c = new ContactItem { FirstName = "Ada" };
        c.Categories.Add(new ContactCategory { Id = "1", Name = "Work" });
        c.Categories.Add(new ContactCategory { Id = "2", Name = "Friends" });
        c.Children.Add(new ContactChild { Id = "3", Name = "Byron" });

        var xml = Xml(c.ToApplicationData());

        Assert.Contains("<Category>Work</Category>", xml);
        Assert.Contains("<Category>Friends</Category>", xml);
        Assert.Contains("<Child>Byron</Child>", xml);
    }

    [Fact]
    public void Contact_without_collections_omits_the_containers()
    {
        var xml = Xml(new ContactItem { FirstName = "Ada" }.ToApplicationData());

        Assert.DoesNotContain("<Categories", xml);
        Assert.DoesNotContain("<Children", xml);
    }

    [Fact]
    public void Calendar_attendees_are_serialized_with_status_and_type()
    {
        var cal = new CalendarItem { Subject = "Standup", StartTime = Ts(2026, 8, 6, 9, 0) };
        cal.Attendees.Add(new CalendarAttendee
        {
            Id = "1",
            Email = "ada@example.com",
            Name = "Ada Lovelace",
            AttendeeStatus = 3,
            AttendeeType = 1,
        });

        var xml = Xml(cal.ToApplicationData());

        Assert.Contains("<Email>ada@example.com</Email>", xml);
        Assert.Contains("<Name>Ada Lovelace</Name>", xml);
        Assert.Contains("<AttendeeStatus>3</AttendeeStatus>", xml);
        Assert.Contains("<AttendeeType>1</AttendeeType>", xml);
    }

    [Fact]
    public void Calendar_categories_are_serialized()
    {
        var cal = new CalendarItem { Subject = "Standup" };
        cal.Categories.Add(new CalendarCategory { Id = "1", Category = "Team" });

        Assert.Contains("<Category>Team</Category>", Xml(cal.ToApplicationData()));
    }

    [Fact]
    public void Calendar_exceptions_are_serialized_including_nested_collections()
    {
        var cal = new CalendarItem { Subject = "Weekly", RecurrenceType = 1, RecurrenceDayOfWeek = 2 };
        var ex = new CalendarException
        {
            Id = "e1",
            ExceptionStartTime = "20260813T090000Z",
            Subject = "Moved",
            StartTime = Ts(2026, 8, 13, 10, 0),
            EndTime = Ts(2026, 8, 13, 11, 0),
        };
        ex.Attendees.Add(new CalendarExceptionAttendee
        { Id = "a1", CalendarExceptionId = "e1", Email = "grace@example.com", Name = "Grace" });
        ex.Categories.Add(new CalendarExceptionCategory
        { Id = "c1", CalendarExceptionId = "e1", Category = "Rescheduled" });
        cal.Exceptions.Add(ex);

        var xml = Xml(cal.ToApplicationData());

        Assert.Contains("<Exceptions", xml);
        Assert.Contains("<ExceptionStartTime>20260813T090000Z</ExceptionStartTime>", xml);
        Assert.Contains("<Subject>Moved</Subject>", xml);
        Assert.Contains("<Email>grace@example.com</Email>", xml);
        Assert.Contains("<Category>Rescheduled</Category>", xml);
        // exception times are compact, same as the top-level ones
        Assert.Contains("20260813T100000Z", xml);
        Assert.DoesNotContain("2026-08-13T", xml);
    }

    // --- merge: device -> server ----------------------------------------------------

    [Fact]
    public void ApplyContactChange_replaces_categories_when_the_container_is_present()
    {
        var existing = new ContactItem { FirstName = "Ada" };
        existing.Categories.Add(new ContactCategory { Id = "1", Name = "Work" });

        var result = ItemSyncService.ApplyContactChange(existing, new ContactData
        {
            Categories = new ContactCategories { Items = ["Friends"] },
        });

        Assert.Equal(["Friends"], result.Categories.Select(x => x.Name));
    }

    [Fact]
    public void ApplyContactChange_clears_categories_when_the_container_is_empty()
    {
        var existing = new ContactItem();
        existing.Categories.Add(new ContactCategory { Id = "1", Name = "Work" });

        var result = ItemSyncService.ApplyContactChange(existing, new ContactData
        {
            Categories = new ContactCategories { Items = [] },
        });

        Assert.Empty(result.Categories);
    }

    [Fact]
    public void ApplyContactChange_preserves_categories_when_the_container_is_absent()
    {
        var existing = new ContactItem();
        existing.Categories.Add(new ContactCategory { Id = "1", Name = "Work" });
        existing.Children.Add(new ContactChild { Id = "2", Name = "Byron" });

        var result = ItemSyncService.ApplyContactChange(existing, new ContactData { FirstName = "Ada" });

        Assert.Equal(["Work"], result.Categories.Select(x => x.Name));
        Assert.Equal(["Byron"], result.Children.Select(x => x.Name));
    }

    [Fact]
    public void ApplyCalendarChange_replaces_attendees_when_the_container_is_present()
    {
        var existing = new CalendarItem { Subject = "Standup" };
        existing.Attendees.Add(new CalendarAttendee { Id = "1", Email = "old@example.com", Name = "Old" });

        var result = ItemSyncService.ApplyCalendarChange(existing, new CalendarData
        {
            Attendees = new CalendarAttendees
            {
                Items = [new CalendarAttendeeData { Email = "new@example.com", Name = "New", AttendeeStatus = 3 }],
            },
        });

        var only = Assert.Single(result.Attendees);
        Assert.Equal("new@example.com", only.Email);
        Assert.Equal(3u, only.AttendeeStatus);
    }

    [Fact]
    public void ApplyCalendarChange_preserves_collections_when_the_containers_are_absent()
    {
        var existing = new CalendarItem { Subject = "Standup" };
        existing.Attendees.Add(new CalendarAttendee { Id = "1", Email = "ada@example.com", Name = "Ada" });
        existing.Categories.Add(new CalendarCategory { Id = "2", Category = "Team" });
        existing.Exceptions.Add(new CalendarException { Id = "3", ExceptionStartTime = "20260813T090000Z" });

        var result = ItemSyncService.ApplyCalendarChange(existing, new CalendarData { Subject = "Renamed" });

        Assert.Equal("Renamed", result.Subject);
        Assert.Single(result.Attendees);
        Assert.Single(result.Categories);
        Assert.Single(result.Exceptions);
    }

    [Fact]
    public void ApplyCalendarChange_replaces_exceptions_when_the_container_is_present()
    {
        var existing = new CalendarItem { Subject = "Weekly" };
        existing.Exceptions.Add(new CalendarException { Id = "old", ExceptionStartTime = "20260806T090000Z" });

        var result = ItemSyncService.ApplyCalendarChange(existing, new CalendarData
        {
            Exceptions = new CalendarExceptions
            {
                Items = [new CalendarExceptionData { ExceptionStartTime = "20260813T090000Z", Subject = "Moved" }],
            },
        });

        var only = Assert.Single(result.Exceptions);
        Assert.Equal("20260813T090000Z", only.ExceptionStartTime);
        Assert.Equal("Moved", only.Subject);
    }

    // --- malformed / partial client input -------------------------------------------

    // protobuf's optional-string setters throw on null, so every unguarded assignment from a
    // nullable model property is a 500 waiting to happen. InstanceId is 16.x-only and absent from
    // every WP7 request, so an unguarded assignment there broke any exception a real device sent.
    [Fact]
    public void Exception_without_the_optional_16x_fields_does_not_throw()
    {
        var cd = new CalendarData
        {
            Subject = "Weekly",
            Exceptions = new CalendarExceptions
            {
                Items = [new CalendarExceptionData { ExceptionStartTime = "20260813T090000Z" }],
            },
        };

        var proto = cd.ToProtoCalendar();

        var only = Assert.Single(proto.Exceptions);
        Assert.Equal("20260813T090000Z", only.ExceptionStartTime);
        Assert.False(only.HasInstanceId);
        Assert.False(only.HasSubject);
        Assert.False(only.HasLocation);
    }

    [Fact]
    public void Attendee_without_an_email_is_dropped_rather_than_throwing()
    {
        var cd = new CalendarData
        {
            Attendees = new CalendarAttendees
            {
                Items =
                [
                    new CalendarAttendeeData { Name = "No Address" },
                    new CalendarAttendeeData { Email = "ada@example.com" },
                ],
            },
        };

        var proto = cd.ToProtoCalendar();

        var only = Assert.Single(proto.Attendees);
        Assert.Equal("ada@example.com", only.Email);
        // Name is required by spec; fall back to the address rather than emit an invalid item
        Assert.Equal("ada@example.com", only.Name);
    }

    // --- full loop ------------------------------------------------------------------

    // What the device actually experiences: send an item, get it back on the next sync.
    [Fact]
    public void Attendees_survive_the_client_to_store_to_client_loop()
    {
        var fromClient = new CalendarData
        {
            Subject = "Review",
            Attendees = new CalendarAttendees
            {
                Items =
                [
                    new CalendarAttendeeData { Email = "ada@example.com", Name = "Ada", AttendeeType = 1 },
                    new CalendarAttendeeData { Email = "grace@example.com", Name = "Grace", AttendeeType = 2 },
                ],
            },
        };

        var xml = Xml(fromClient.ToProtoCalendar().ToApplicationData());

        Assert.Contains("<Email>ada@example.com</Email>", xml);
        Assert.Contains("<Email>grace@example.com</Email>", xml);
        Assert.Contains("<AttendeeType>1</AttendeeType>", xml);
        Assert.Contains("<AttendeeType>2</AttendeeType>", xml);
    }
}
