using System.Xml;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Tests;

public class GhostingTests
{
    private static SyncSupported Supported(params (string Ns, string Name)[] elements)
    {
        var doc = new XmlDocument();
        var s = new SyncSupported();
        foreach (var (ns, name) in elements)
            s.Elements.Add(doc.CreateElement(name, ns));
        return s;
    }

    private static GhostingPolicy PolicyFor(params (string Ns, string Name)[] elements) =>
        GhostingPolicy.FromSupported(Supported(elements));

    private static ContactItem FullContact() => new()
    {
        FirstName = "Ada",
        LastName = "Lovelace",
        CompanyName = "Analytical Engines",
        JobTitle = "Engineer",
        MobilePhoneNumber = "+441234567890",
    };

    // --- the fallback ----------------------------------------------------------------

    [Fact]
    public void Absent_Supported_preserves_every_omitted_element()
    {
        var policy = GhostingPolicy.FromSupported(null);

        var result = ItemSyncService.ApplyContactChange(FullContact(),
            new ContactData { FirstName = "Ada B" }, policy);

        Assert.True(policy.IsPreserveAll);
        Assert.Equal("Ada B", result.FirstName);
        Assert.Equal("Lovelace", result.LastName);
        Assert.Equal("Analytical Engines", result.CompanyName);
        Assert.Equal("Engineer", result.JobTitle);
        Assert.Equal("+441234567890", result.MobilePhoneNumber);
    }

    // rule 2 in the spec: an empty Supported ghosts everything, which is also what we fall back to
    [Fact]
    public void Empty_Supported_preserves_every_omitted_element()
    {
        var policy = GhostingPolicy.FromSupported(new SyncSupported());

        var result = ItemSyncService.ApplyContactChange(FullContact(),
            new ContactData { FirstName = "Ada B" }, policy);

        Assert.True(policy.IsPreserveAll);
        Assert.Equal("Lovelace", result.LastName);
        Assert.Equal("Analytical Engines", result.CompanyName);
    }

    [Fact]
    public void Default_overload_preserves_everything()
    {
        var result = ItemSyncService.ApplyContactChange(FullContact(), new ContactData { FirstName = "Ada B" });

        Assert.Equal("Lovelace", result.LastName);
        Assert.Equal("Analytical Engines", result.CompanyName);
    }

    // --- declared elements -----------------------------------------------------------

    [Fact]
    public void Declared_element_is_cleared_when_omitted()
    {
        var policy = PolicyFor((Constants.Contacts, "CompanyName"));

        var result = ItemSyncService.ApplyContactChange(FullContact(),
            new ContactData { FirstName = "Ada B" }, policy);

        Assert.False(result.HasCompanyName);
    }

    [Fact]
    public void Undeclared_elements_stay_ghosted_even_when_others_are_declared()
    {
        var policy = PolicyFor((Constants.Contacts, "CompanyName"));

        var result = ItemSyncService.ApplyContactChange(FullContact(),
            new ContactData { FirstName = "Ada B" }, policy);

        // only CompanyName was declared, so everything else survives the omission
        Assert.Equal("Lovelace", result.LastName);
        Assert.Equal("Engineer", result.JobTitle);
        Assert.Equal("+441234567890", result.MobilePhoneNumber);
    }

    [Fact]
    public void Declared_element_that_is_sent_is_updated_not_cleared()
    {
        var policy = PolicyFor((Constants.Contacts, "CompanyName"));

        var result = ItemSyncService.ApplyContactChange(FullContact(),
            new ContactData { CompanyName = "Difference Engines" }, policy);

        Assert.Equal("Difference Engines", result.CompanyName);
    }

    // Subject, Location, Body and Categories all exist in more than one class namespace, so the
    // match has to be namespace-qualified or a calendar declaration would clear a contact field
    [Fact]
    public void Matching_is_namespace_qualified()
    {
        var policy = PolicyFor((Constants.Calendar, "Categories"));
        var contact = new ContactItem { FirstName = "Ada" };
        contact.Categories.Add(new ContactCategory { Id = "1", Name = "Work" });

        var result = ItemSyncService.ApplyContactChange(contact, new ContactData { FirstName = "Ada" }, policy);

        Assert.Single(result.Categories);
    }

    // --- calendar --------------------------------------------------------------------

    private static CalendarItem FullCalendar()
    {
        var cal = new CalendarItem { Subject = "Standup", Location = "Room 1", Timezone = "tz" };
        cal.Attendees.Add(new CalendarAttendee { Id = "1", Email = "ada@example.com", Name = "Ada" });
        cal.Categories.Add(new CalendarCategory { Id = "2", Category = "Team" });
        return cal;
    }

    [Fact]
    public void Calendar_declared_location_is_cleared_when_omitted()
    {
        var policy = PolicyFor((Constants.Calendar, "Location"));

        var result = ItemSyncService.ApplyCalendarChange(FullCalendar(),
            new CalendarData { Subject = "Standup" }, policy);

        Assert.False(result.HasLocation);
        Assert.Equal("Standup", result.Subject);
        Assert.Equal("tz", result.Timezone);
    }

    [Fact]
    public void Calendar_declared_collections_are_cleared_when_omitted()
    {
        var policy = PolicyFor((Constants.Calendar, "Attendees"), (Constants.Calendar, "Categories"));

        var result = ItemSyncService.ApplyCalendarChange(FullCalendar(),
            new CalendarData { Subject = "Standup" }, policy);

        Assert.Empty(result.Attendees);
        Assert.Empty(result.Categories);
    }

    [Fact]
    public void Calendar_undeclared_collections_survive_omission()
    {
        var policy = PolicyFor((Constants.Calendar, "Location"));

        var result = ItemSyncService.ApplyCalendarChange(FullCalendar(),
            new CalendarData { Subject = "Standup" }, policy);

        Assert.Single(result.Attendees);
        Assert.Single(result.Categories);
    }

    [Fact]
    public void Calendar_declared_recurrence_clears_the_whole_block()
    {
        var policy = PolicyFor((Constants.Calendar, "Recurrence"));
        var cal = new CalendarItem
        {
            Subject = "Weekly",
            RecurrenceType = 1,
            RecurrenceInterval = 2,
            RecurrenceDayOfWeek = 4,
        };

        var result = ItemSyncService.ApplyCalendarChange(cal, new CalendarData { Subject = "Weekly" }, policy);

        Assert.False(result.HasRecurrenceType);
        Assert.False(result.HasRecurrenceInterval);
        Assert.False(result.HasRecurrenceDayOfWeek);
    }

    // --- caching round-trip ----------------------------------------------------------

    [Fact]
    public void Policy_survives_serialize_and_parse()
    {
        var original = PolicyFor((Constants.Contacts, "CompanyName"), (Constants.Calendar, "Location"));

        var restored = GhostingPolicy.Parse(original.Serialize());

        Assert.True(restored.ShouldClear(Constants.Contacts, "CompanyName"));
        Assert.True(restored.ShouldClear(Constants.Calendar, "Location"));
        Assert.True(restored.IsGhosted(Constants.Contacts, "LastName"));
        Assert.False(restored.IsPreserveAll);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Missing_cached_policy_parses_back_to_preserve_all(string? stored)
    {
        var policy = GhostingPolicy.Parse(stored);

        Assert.True(policy.IsPreserveAll);
        Assert.True(policy.IsGhosted(Constants.Contacts, "CompanyName"));
    }

    [Fact]
    public void PreserveAll_serializes_to_nothing()
    {
        Assert.Equal(string.Empty, GhostingPolicy.PreserveAll.Serialize());
    }
}
