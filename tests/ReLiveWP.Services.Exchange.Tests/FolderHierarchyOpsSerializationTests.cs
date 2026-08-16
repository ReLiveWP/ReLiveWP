using System.Xml;
using System.Xml.Serialization;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Tests;

// response element order is part of the contract, and SyncKey/ServerId must be absent on failure;
// these pin both at the same boundary the controllers use: XmlSerializer -> ASWBXML -> bytes -> back
public class FolderHierarchyOpsSerializationTests
{
    private static T Deserialize<T>(string xml) where T : class
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        return (T)new XmlSerializer(typeof(T)).Deserialize(new XmlNodeReader(doc))!;
    }

    // mirrors ActiveSyncCommandController.WriteWbxmlResponseAsync so these assert what the device receives
    private static XmlElement RoundTrip<T>(T response) where T : class
    {
        using var writer = new StringWriter();
        new XmlSerializer(typeof(T)).Serialize(writer, response);

        var encoder = new ASWBXML();
        encoder.LoadXml(writer.ToString());

        var decoder = new ASWBXML();
        decoder.LoadBytes(encoder.GetBytes());
        return decoder.GetXmlDocument().DocumentElement!;
    }

    // decoded elements are prefixed (folderhierarchy:Status), so match on LocalName
    private static string[] Order(XmlElement root) =>
        [.. root.ChildNodes.OfType<XmlElement>().Select(e => e.LocalName)];

    private static string? Value(XmlElement root, string localName) =>
        root.ChildNodes.OfType<XmlElement>().FirstOrDefault(e => e.LocalName == localName)?.InnerText;

    [Fact]
    public void FolderCreate_request_from_device_deserializes()
    {
        // verbatim from Outlook for Android creating an "Archive" mail folder
        var request = Deserialize<FolderCreate>(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <folderhierarchy:FolderCreate xmlns:folderhierarchy="FolderHierarchy">
              <folderhierarchy:SyncKey>1</folderhierarchy:SyncKey>
              <folderhierarchy:ParentId>0</folderhierarchy:ParentId>
              <folderhierarchy:DisplayName>Archive</folderhierarchy:DisplayName>
              <folderhierarchy:Type>12</folderhierarchy:Type>
            </folderhierarchy:FolderCreate>
            """);

        Assert.Equal("1", request.SyncKey);
        Assert.Equal("0", request.ParentId);
        Assert.Equal("Archive", request.DisplayName);
        Assert.Equal(FolderType.Mail, request.Type);
    }

    [Fact]
    public void FolderCreate_response_emits_Status_SyncKey_ServerId_in_order()
    {
        var root = RoundTrip(new FolderCreateResponse
        {
            Status = 1,
            SyncKey = "2",
            ServerId = "092a852511c34b938fe605c66655fde8",
        });

        Assert.Equal("FolderCreate", root.LocalName);
        Assert.Equal(["Status", "SyncKey", "ServerId"], Order(root));
        Assert.Equal("1", Value(root, "Status"));
        Assert.Equal("2", Value(root, "SyncKey"));
        Assert.Equal("092a852511c34b938fe605c66655fde8", Value(root, "ServerId"));
    }

    [Theory]
    [InlineData(FolderSyncService.StatusInvalidSyncKey)]
    [InlineData(FolderSyncService.StatusMalformed)]
    [InlineData(FolderSyncService.StatusParentNotFound)]
    [InlineData(FolderSyncService.StatusNameExistsOrSpecial)]
    public void FolderCreate_failure_response_omits_SyncKey_and_ServerId(int status)
    {
        var root = RoundTrip(new FolderCreateResponse { Status = status });

        Assert.Equal(["Status"], Order(root));
        Assert.Equal(status.ToString(), Value(root, "Status"));
    }

    [Fact]
    public void FolderUpdate_request_from_device_deserializes()
    {
        var request = Deserialize<FolderUpdate>(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <folderhierarchy:FolderUpdate xmlns:folderhierarchy="FolderHierarchy">
              <folderhierarchy:SyncKey>2</folderhierarchy:SyncKey>
              <folderhierarchy:ServerId>092a852511c34b938fe605c66655fde8</folderhierarchy:ServerId>
              <folderhierarchy:ParentId>0</folderhierarchy:ParentId>
              <folderhierarchy:DisplayName>Archived</folderhierarchy:DisplayName>
            </folderhierarchy:FolderUpdate>
            """);

        Assert.Equal("2", request.SyncKey);
        Assert.Equal("092a852511c34b938fe605c66655fde8", request.ServerId);
        Assert.Equal("0", request.ParentId);
        Assert.Equal("Archived", request.DisplayName);
    }

    [Fact]
    public void FolderDelete_request_from_device_deserializes()
    {
        var request = Deserialize<FolderDelete>(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <folderhierarchy:FolderDelete xmlns:folderhierarchy="FolderHierarchy">
              <folderhierarchy:SyncKey>3</folderhierarchy:SyncKey>
              <folderhierarchy:ServerId>092a852511c34b938fe605c66655fde8</folderhierarchy:ServerId>
            </folderhierarchy:FolderDelete>
            """);

        Assert.Equal("3", request.SyncKey);
        Assert.Equal("092a852511c34b938fe605c66655fde8", request.ServerId);
    }

    [Fact]
    public void FolderUpdate_and_FolderDelete_responses_emit_Status_before_SyncKey()
    {
        var update = RoundTrip(new FolderUpdateResponse { Status = 1, SyncKey = "3" });
        Assert.Equal("FolderUpdate", update.LocalName);
        Assert.Equal(["Status", "SyncKey"], Order(update));

        var delete = RoundTrip(new FolderDeleteResponse { Status = 1, SyncKey = "4" });
        Assert.Equal("FolderDelete", delete.LocalName);
        Assert.Equal(["Status", "SyncKey"], Order(delete));

        Assert.Equal(["Status"], Order(RoundTrip(new FolderUpdateResponse { Status = 4 })));
        Assert.Equal(["Status"], Order(RoundTrip(new FolderDeleteResponse { Status = 3 })));
    }
}
