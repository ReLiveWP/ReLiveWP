using System.Xml;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Tests;

/// <summary>
/// Every element a client may legally name in Supported has to actually reach the clear pass with
/// the namespace it arrives under. Comparing a declared-policy result against the ghost-all
/// baseline catches a mis-keyed entry without needing an element-to-protobuf-field map.
/// </summary>
public class GhostableVocabularyTests
{
    private static GhostingPolicy PolicyFor(string ns, string name)
    {
        var doc = new XmlDocument();
        var s = new SyncSupported();
        s.Elements.Add(doc.CreateElement(name, ns));
        return GhostingPolicy.FromSupported(s);
    }

    private static ContactItem PopulatedContact()
    {
        var c = new ContactItem();
        foreach (var f in ContactItem.Descriptor.Fields.InDeclarationOrder())
            Populate(c, f);

        c.Categories.Add(new ContactCategory { Id = "1", Name = "Work" });
        c.Children.Add(new ContactChild { Id = "2", Name = "Byron" });
        return c;
    }

    private static CalendarItem PopulatedCalendar()
    {
        var cal = new CalendarItem();
        foreach (var f in CalendarItem.Descriptor.Fields.InDeclarationOrder())
            Populate(cal, f);

        cal.Attendees.Add(new CalendarAttendee { Id = "1", Email = "ada@example.com", Name = "Ada" });
        cal.Categories.Add(new CalendarCategory { Id = "2", Category = "Team" });
        cal.Exceptions.Add(new CalendarException { Id = "3" });
        return cal;
    }

    private static void Populate(IMessage msg, Google.Protobuf.Reflection.FieldDescriptor f)
    {
        if (f.IsRepeated || f.IsMap) return;

        object? value = f.FieldType switch
        {
            Google.Protobuf.Reflection.FieldType.String => "x",
            Google.Protobuf.Reflection.FieldType.Bool => true,
            Google.Protobuf.Reflection.FieldType.UInt32 => 1u,
            Google.Protobuf.Reflection.FieldType.Int32 => 1,
            Google.Protobuf.Reflection.FieldType.Bytes => ByteString.CopyFrom(1, 2, 3),
            Google.Protobuf.Reflection.FieldType.Message when f.MessageType.ClrType == typeof(Timestamp) =>
                Timestamp.FromDateTime(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            _ => null,
        };

        if (value is not null) f.Accessor.SetValue(msg, value);
    }

    [Theory]
    [MemberData(nameof(GhostableVocabulary.ContactRows), MemberType = typeof(GhostableVocabulary))]
    public void Declared_contact_element_reaches_the_clear_pass(string ns, string element)
    {
        var baseline = ItemSyncService.ApplyContactChange(
            PopulatedContact(), new ContactData(), GhostingPolicy.GhostAll);

        var declared = ItemSyncService.ApplyContactChange(
            PopulatedContact(), new ContactData(), PolicyFor(ns, element));

        Assert.False(baseline.Equals(declared),
            $"{ns}:{element} is in the spec's ghosting group but declaring it changed nothing, " +
            "so the clear pass never matched it");
    }

    [Theory]
    [MemberData(nameof(GhostableVocabulary.CalendarRows), MemberType = typeof(GhostableVocabulary))]
    public void Declared_calendar_element_reaches_the_clear_pass(string ns, string element)
    {
        var baseline = ItemSyncService.ApplyCalendarChange(
            PopulatedCalendar(), new CalendarData(), GhostingPolicy.GhostAll);

        var declared = ItemSyncService.ApplyCalendarChange(
            PopulatedCalendar(), new CalendarData(), PolicyFor(ns, element));

        Assert.False(baseline.Equals(declared),
            $"{ns}:{element} is in the spec's ghosting group but declaring it changed nothing, " +
            "so the clear pass never matched it");
    }

    [Fact]
    public void Ghost_all_clears_nothing_at_all()
    {
        var contact = PopulatedContact();
        var calendar = PopulatedCalendar();

        Assert.Equal(contact, ItemSyncService.ApplyContactChange(
            contact.Clone(), new ContactData(), GhostingPolicy.GhostAll));
        Assert.Equal(calendar, ItemSyncService.ApplyCalendarChange(
            calendar.Clone(), new CalendarData(), GhostingPolicy.GhostAll));
    }

    // contacts:Alias is a recipient-cache property, not part of contacts:GhostingProps, so the
    // ghost-none sweep must leave it alone
    [Fact]
    public void Non_ghostable_contact_elements_survive_ghost_none()
    {
        var result = ItemSyncService.ApplyContactChange(
            PopulatedContact(), new ContactData(), GhostingPolicy.GhostNone);

        Assert.True(result.HasAlias);
        Assert.True(result.HasPicture);
        Assert.True(result.HasNotes);
    }

    [Fact]
    public void Ghost_none_clears_the_whole_contact_vocabulary()
    {
        var result = ItemSyncService.ApplyContactChange(
            PopulatedContact(), new ContactData(), GhostingPolicy.GhostNone);

        Assert.False(result.HasFirstName);
        Assert.False(result.HasNickName);
        Assert.False(result.HasImAddress);
        Assert.Empty(result.Categories);
        Assert.Empty(result.Children);
    }

    [Fact]
    public void Undeclared_vocabulary_element_survives()
    {
        var result = ItemSyncService.ApplyContactChange(
            PopulatedContact(), new ContactData(), PolicyFor(Constants.Contacts, "CompanyName"));

        Assert.False(result.HasCompanyName);
        Assert.True(result.HasFirstName);
        Assert.True(result.HasNickName);
    }
}
