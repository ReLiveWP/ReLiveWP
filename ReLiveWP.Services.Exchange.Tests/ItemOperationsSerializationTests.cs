using System.Xml;
using System.Xml.Serialization;
using ReLiveWP.Services.Exchange.Extensions;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Tests;

// pins both the wire shape the device sends and the shape the server must emit
public class ItemOperationsSerializationTests
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

    private static string[] Order(XmlElement root) =>
        [.. root.ChildNodes.OfType<XmlElement>().Select(e => e.LocalName)];

    private static string? Value(XmlElement root, string localName) =>
        root.ChildNodes.OfType<XmlElement>().FirstOrDefault(e => e.LocalName == localName)?.InnerText;

    private static XmlElement? Child(XmlElement root, string localName) =>
        root.ChildNodes.OfType<XmlElement>().FirstOrDefault(e => e.LocalName == localName);

    [Fact]
    public void Fetch_request_with_FileReference_and_Range_deserializes()
    {
        var request = Deserialize<ItemOperations>(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <ItemOperations xmlns="ItemOperations" xmlns:airsyncbase="AirSyncBase">
              <Fetch>
                <Store>Mailbox</Store>
                <airsyncbase:FileReference>a1b2c3</airsyncbase:FileReference>
                <Options>
                  <Range>0-4095</Range>
                </Options>
              </Fetch>
            </ItemOperations>
            """);

        var fetch = Assert.Single(request.Fetch);
        Assert.Equal("Mailbox", fetch.Store);
        Assert.Equal("a1b2c3", fetch.FileReference);
        Assert.Null(fetch.ServerId);
        Assert.Equal("0-4095", fetch.Options?.Range);
    }

    [Fact]
    public void Fetch_request_with_ServerId_and_BodyPartPreference_deserializes()
    {
        var request = Deserialize<ItemOperations>(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <ItemOperations xmlns="ItemOperations" xmlns:airsync="AirSync" xmlns:airsyncbase="AirSyncBase">
              <Fetch>
                <Store>Mailbox</Store>
                <airsync:CollectionId>1</airsync:CollectionId>
                <airsync:ServerId>abc123</airsync:ServerId>
                <Options>
                  <airsyncbase:BodyPartPreference>
                    <airsyncbase:Type>2</airsyncbase:Type>
                  </airsyncbase:BodyPartPreference>
                </Options>
              </Fetch>
            </ItemOperations>
            """);

        var fetch = Assert.Single(request.Fetch);
        Assert.Equal("abc123", fetch.ServerId);
        Assert.Null(fetch.FileReference);
        var pref = Assert.Single(fetch.Options!.BodyPartPreference);
        Assert.Equal(BodyType.HTML, pref.Type);
        Assert.Empty(fetch.Options!.BodyPreference);
    }

    [Fact]
    public void Attachment_fetch_response_emits_Status_FileReference_Properties_in_order()
    {
        var root = RoundTrip(new ItemOperationsResponse
        {
            Status = 1,
            Response = new ItemOperationsResponseBody
            {
                Fetch =
                [
                    new ItemOperationsFetchResponse
                    {
                        Status = 1,
                        FileReference = "a1b2c3",
                        Properties = new ItemOperationsProperties
                        {
                            Range = "0-9",
                            Total = 26,
                            ContentType = "text/plain",
                            Data = "SGVsbG8gd29ybGQ=",
                        },
                    },
                ],
            },
        });

        var fetchResponse = Child(Child(root, "Response")!, "Fetch")!;
        Assert.Equal(["Status", "FileReference", "Properties"], Order(fetchResponse));

        var props = Child(fetchResponse, "Properties")!;
        Assert.Equal(["Range", "Total", "ContentType", "Data"], Order(props));
        Assert.Equal("0-9", Value(props, "Range"));
        Assert.Equal("26", Value(props, "Total"));
        Assert.Equal("text/plain", Value(props, "ContentType"));
        Assert.Equal("SGVsbG8gd29ybGQ=", Value(props, "Data"));
    }

    [Fact]
    public void Pim_fetch_response_omits_FileReference_and_Range_Total_Data()
    {
        var root = RoundTrip(new ItemOperationsResponse
        {
            Status = 1,
            Response = new ItemOperationsResponseBody
            {
                Fetch =
                [
                    new ItemOperationsFetchResponse
                    {
                        Status = 1,
                        ServerId = "srv1",
                        Class = "Email",
                        Properties = new ItemOperationsProperties(),
                    },
                ],
            },
        });

        var fetchResponse = Child(Child(root, "Response")!, "Fetch")!;
        Assert.DoesNotContain("FileReference", Order(fetchResponse));

        var props = Child(fetchResponse, "Properties")!;
        Assert.DoesNotContain("Range", Order(props));
        Assert.DoesNotContain("Total", Order(props));
        Assert.DoesNotContain("Data", Order(props));
    }

    [Fact]
    public void EmailItem_with_attachments_serializes_AirSyncBase_Attachments()
    {
        var email = new EmailItem { Subject = "Has attachment" };
        email.Attachments.Add(new AttachmentItem
        {
            Id = "att-1",
            DisplayName = "photo.jpg",
            ContentType = "image/jpeg",
            EstimatedDataSize = 12345,
            Method = 1,
            IsInline = true,
        });

        var appData = email.ToApplicationData();
        var xml = string.Concat(appData.Elements.Select(e => e.OuterXml));

        Assert.Contains("<Attachments", xml);
        Assert.Contains("<FileReference>att-1</FileReference>", xml);
        Assert.Contains("<DisplayName>photo.jpg</DisplayName>", xml);
        Assert.Contains("<Method>1</Method>", xml);
        Assert.Contains("<EstimatedDataSize>12345</EstimatedDataSize>", xml);
        // IsInline is an empty-tag element: presence (in any self-closed spelling) means true
        Assert.Matches("<IsInline\\s*/>", xml);
    }

    [Fact]
    public void EmailItem_attachment_without_IsInline_omits_the_element()
    {
        var email = new EmailItem { Subject = "Not inline" };
        email.Attachments.Add(new AttachmentItem { Id = "att-2", Method = 1, EstimatedDataSize = 1 });

        var appData = email.ToApplicationData();
        var xml = string.Concat(appData.Elements.Select(e => e.OuterXml));

        Assert.DoesNotContain("IsInline", xml);
    }

    [Fact]
    public void EmailItem_without_attachments_omits_Attachments_element()
    {
        var email = new EmailItem { Subject = "No attachment" };

        var appData = email.ToApplicationData();
        var xml = string.Concat(appData.Elements.Select(e => e.OuterXml));

        Assert.DoesNotContain("Attachments", xml);
    }

    [Fact]
    public void Move_request_with_MoveAlways_deserializes()
    {
        var request = Deserialize<ItemOperations>(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <ItemOperations xmlns="ItemOperations">
              <Move>
                <ConversationId>aGVsbG8=</ConversationId>
                <DstFldId>2</DstFldId>
                <Options>
                  <MoveAlways/>
                </Options>
              </Move>
            </ItemOperations>
            """);

        var move = Assert.Single(request.Move);
        Assert.Equal("hello"u8.ToArray(), move.ConversationId);
        Assert.Equal("2", move.DstFldId);
        Assert.NotNull(move.Options?.MoveAlways);
    }

    [Fact]
    public void Move_request_without_Options_leaves_MoveAlways_absent()
    {
        var request = Deserialize<ItemOperations>(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <ItemOperations xmlns="ItemOperations">
              <Move>
                <ConversationId>aGVsbG8=</ConversationId>
                <DstFldId>2</DstFldId>
              </Move>
            </ItemOperations>
            """);

        var move = Assert.Single(request.Move);
        Assert.Null(move.Options);
    }

    [Fact]
    public void EmptyFolderContents_response_omits_Store()
    {
        // Store is request-only; echoing it back in the response breaks strict clients
        var root = RoundTrip(new ItemOperationsResponse
        {
            Status = 1,
            Response = new ItemOperationsResponseBody
            {
                EmptyFolderContents =
                [
                    new ItemOperationsEmptyFolderResponse { Status = 1, CollectionId = "15" },
                ],
            },
        });

        var response = Child(Child(root, "Response")!, "EmptyFolderContents")!;
        Assert.Equal(["Status", "CollectionId"], Order(response));
    }

    [Fact]
    public void Move_response_round_trips_ConversationId_and_Status()
    {
        var root = RoundTrip(new ItemOperationsResponse
        {
            Status = 1,
            Response = new ItemOperationsResponseBody
            {
                Move =
                [
                    new ItemOperationsMoveResponse
                    {
                        ConversationId = "hello"u8.ToArray(),
                        Status = 1,
                    },
                ],
            },
        });

        var moveResponse = Child(Child(root, "Response")!, "Move")!;
        Assert.Equal(["ConversationId", "Status"], Order(moveResponse));
        Assert.Equal(Convert.ToBase64String("hello"u8.ToArray()), Value(moveResponse, "ConversationId"));
        Assert.Equal("1", Value(moveResponse, "Status"));
    }
}
