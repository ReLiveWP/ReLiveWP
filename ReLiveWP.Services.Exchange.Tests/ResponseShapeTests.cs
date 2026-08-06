using System.Text.RegularExpressions;
using System.Xml.Serialization;
using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Tests;

public class ResponseShapeTests
{
    private static string Serialize<T>(T value) where T : class
    {
        using var writer = new StringWriter();
        new XmlSerializer(typeof(T)).Serialize(writer, value);
        return writer.ToString();
    }

    // elements carry namespace declarations, so match on the local name only
    private static int IndexOfTag(string xml, string localName)
    {
        var m = Regex.Match(xml, $"<{Regex.Escape(localName)}[ />]");
        return m.Success ? m.Index : -1;
    }

    private static string? TextOf(string xml, string localName)
    {
        var m = Regex.Match(xml, $"<{Regex.Escape(localName)}(?:\\s[^>]*)?>([^<]*)</{Regex.Escape(localName)}>");
        return m.Success ? m.Groups[1].Value : null;
    }

    // MS-ASCMD 6.15 sequences Changes as Count, Update, Delete, Add. Only matters when a single
    // hierarchy sync carries more than one kind - which is exactly what the orphan-folder
    // reconcile produces (stale Deletes alongside fresh Adds).
    [Fact]
    public void FolderSync_Changes_emits_Count_Update_Delete_Add_in_order()
    {
        var xml = Serialize(new FolderSync
        {
            SyncKey = "2",
            Changes = new Changes
            {
                Count = 3,
                Add = [new FolderChange { ServerId = "a", DisplayName = "Added", Type = FolderType.Mail }],
                Update = [new FolderChange { ServerId = "u", DisplayName = "Updated", Type = FolderType.Mail }],
                Delete = [new FolderChange { ServerId = "d", DisplayName = "Deleted", Type = FolderType.Mail }],
            },
        });

        int count = IndexOfTag(xml, "Count");
        int update = IndexOfTag(xml, "Update");
        int delete = IndexOfTag(xml, "Delete");
        int add = IndexOfTag(xml, "Add");

        Assert.True(count >= 0 && update >= 0 && delete >= 0 && add >= 0, xml);
        Assert.True(count < update, "Count must precede Update");
        Assert.True(update < delete, "Update must precede Delete");
        Assert.True(delete < add, "Delete must precede Add");
    }

    // MS-ASNOTE 2.2.2.4: "a string data type represented as a Compact DateTime"
    [Fact]
    public void Note_LastModifiedDate_uses_compact_datetime()
    {
        var xml = Serialize(new NoteData
        {
            Subject = "n",
            LastModifiedDate = new DateTime(2026, 8, 6, 13, 5, 9, DateTimeKind.Utc),
        });

        Assert.Equal("20260806T130509Z", TextOf(xml, "LastModifiedDate"));
    }

    // MS-ASCMD 2.2.3.168.2 marks the top-level Status as required (1..1)
    [Fact]
    public void Settings_response_carries_a_required_status()
    {
        var xml = Serialize(new SettingsResponse
        {
            UserInformation = new SettingsUserInformationResponse { Status = 1 },
        });

        Assert.Equal("1", TextOf(xml, "Status"));
        Assert.True(IndexOfTag(xml, "Status") < IndexOfTag(xml, "UserInformation"),
            "top-level Status must precede the property containers");
    }

    // The Responses/Add sequence really is Class, ClientId, ServerId, Status (MS-ASCMD 6.46) -
    // the descriptive element table disagrees, and the XSD wins. Pinned so a future "fix" to
    // ServerId-first doesn't regress the element carrying the ClientId->ServerId mapping.
    [Fact]
    public void Sync_response_Add_emits_ClientId_before_ServerId()
    {
        var xml = Serialize(new SyncResponseAdd { ClientId = "c1", ServerId = "s1", Status = 1 });

        Assert.True(IndexOfTag(xml, "ClientId") < IndexOfTag(xml, "ServerId"),
            "ClientId must precede ServerId per the Sync response XSD");
    }

    // On Attachment, IsInline is a boolean (MS-ASAIRS 2.2.2.26.2); the empty-tag form belongs to
    // the 16.x request-side IsInline (Add).
    [Fact]
    public void Attachment_IsInline_is_a_boolean_not_an_empty_tag()
    {
        var inline = Serialize(new AirSyncAttachment { FileReference = "f", IsInline = true });
        var notInline = Serialize(new AirSyncAttachment { FileReference = "f", IsInline = false });

        Assert.Equal("1", TextOf(inline, "IsInline"));
        // optional element: absence still reads as "not inline"
        Assert.Equal(-1, IndexOfTag(notInline, "IsInline"));
    }
}
