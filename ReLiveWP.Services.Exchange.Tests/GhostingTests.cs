using System.Xml;
using Google.Protobuf;
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

    // --- the three rules of MS-ASCMD 2.2.3.179 ---------------------------------------

    // rule 1: no Supported element at all means nothing is ghosted, so omission deletes
    [Fact]
    public void Absent_Supported_is_ghost_none()
    {
        var policy = GhostingPolicy.FromSupported(null);

        Assert.Equal(GhostingMode.GhostNone, policy.Mode);

        var result = ItemSyncService.ApplyContactChange(FullContact(),
            new ContactData { FirstName = "Ada B" }, policy.Effective(true));

        Assert.Equal("Ada B", result.FirstName);
        Assert.False(result.HasLastName);
        Assert.False(result.HasCompanyName);
        Assert.False(result.HasJobTitle);
        Assert.False(result.HasMobilePhoneNumber);
    }

    // rule 1 is destructive and we cannot prove what WP7 sends, so it is gated off by default
    [Fact]
    public void Absent_Supported_still_preserves_when_the_flag_is_off()
    {
        var policy = GhostingPolicy.FromSupported(null).Effective(false);

        var result = ItemSyncService.ApplyContactChange(FullContact(),
            new ContactData { FirstName = "Ada B" }, policy);

        Assert.True(policy.PreservesEverything);
        Assert.Equal("Ada B", result.FirstName);
        Assert.Equal("Lovelace", result.LastName);
        Assert.Equal("Analytical Engines", result.CompanyName);
        Assert.Equal("Engineer", result.JobTitle);
        Assert.Equal("+441234567890", result.MobilePhoneNumber);
    }

    // rule 3: an empty Supported ghosts everything, and the flag must never touch that
    [Fact]
    public void Empty_Supported_preserves_every_omitted_element()
    {
        var policy = GhostingPolicy.FromSupported(new SyncSupported());

        var result = ItemSyncService.ApplyContactChange(FullContact(),
            new ContactData { FirstName = "Ada B" }, policy.Effective(true));

        Assert.Equal(GhostingMode.GhostAll, policy.Mode);
        Assert.True(policy.PreservesEverything);
        Assert.Equal("Lovelace", result.LastName);
        Assert.Equal("Analytical Engines", result.CompanyName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Effective_only_ever_changes_ghost_none(bool flag)
    {
        Assert.Same(GhostingPolicy.GhostAll, GhostingPolicy.GhostAll.Effective(flag));

        var declared = PolicyFor((Constants.Contacts, "CompanyName"));
        Assert.Same(declared, declared.Effective(flag));

        Assert.Equal(flag ? GhostingMode.GhostNone : GhostingMode.GhostAll,
            GhostingPolicy.GhostNone.Effective(flag).Mode);
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
        Assert.Equal(GhostingMode.Declared, restored.Mode);
    }

    // every row written before the three-state encoding holds null or "", and both must keep
    // meaning preserve-everything or an in-flight sync relationship changes behaviour on deploy
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Missing_cached_policy_parses_back_to_ghost_all(string? stored)
    {
        var policy = GhostingPolicy.Parse(stored);

        Assert.True(policy.PreservesEverything);
        Assert.True(policy.IsGhosted(Constants.Contacts, "CompanyName"));
    }

    [Fact]
    public void GhostAll_serializes_to_empty()
    {
        Assert.Equal(string.Empty, GhostingPolicy.GhostAll.Serialize());
    }

    [Fact]
    public void GhostNone_round_trips_through_the_cache()
    {
        var restored = GhostingPolicy.Parse(GhostingPolicy.GhostNone.Serialize());

        Assert.Equal(GhostingMode.GhostNone, restored.Mode);
        Assert.True(restored.ShouldClear(Constants.Contacts, "CompanyName"));
    }

    [Fact]
    public void GhostNone_marker_cannot_collide_with_a_declared_list()
    {
        var marker = GhostingPolicy.GhostNone.Serialize();

        Assert.DoesNotContain(':', marker);
        foreach (var (ns, name) in GhostableVocabulary.All())
            Assert.NotEqual(marker, PolicyFor((ns, name)).Serialize());
    }

    // --- the MS-ASCMD 2.2.3.24 carve-out ---------------------------------------------

    [Fact]
    public void Picture_is_never_cleared_even_when_declared()
    {
        var contact = new ContactItem { FirstName = "Ada", Picture = ByteString.CopyFrom(1, 2, 3) };

        var result = ItemSyncService.ApplyContactChange(contact, new ContactData { FirstName = "Ada" },
            PolicyFor((Constants.Contacts, "Picture")));

        Assert.True(result.HasPicture);
    }

    [Fact]
    public void Picture_survives_ghost_none()
    {
        var contact = new ContactItem { FirstName = "Ada", Picture = ByteString.CopyFrom(1, 2, 3) };

        var result = ItemSyncService.ApplyContactChange(contact, new ContactData { FirstName = "Ada" },
            GhostingPolicy.GhostNone);

        Assert.True(result.HasPicture);
    }

    [Fact]
    public void Contact_notes_survive_ghost_none()
    {
        var contact = new ContactItem { FirstName = "Ada", Notes = "kept" };

        var result = ItemSyncService.ApplyContactChange(contact, new ContactData { FirstName = "Ada" },
            GhostingPolicy.GhostNone);

        Assert.Equal("kept", result.Notes);
    }

    [Fact]
    public void Calendar_notes_survive_ghost_none()
    {
        var cal = new CalendarItem { Subject = "Standup", Notes = "kept" };

        var result = ItemSyncService.ApplyCalendarChange(cal, new CalendarData { Subject = "Standup" },
            GhostingPolicy.GhostNone);

        Assert.Equal("kept", result.Notes);
    }
}
