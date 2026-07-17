using System.Security.Claims;
using System.Text;
using System.Xml;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Exchange.Controllers;
using ReLiveWP.Services.Exchange.Middleware;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Tests;

// end-to-end through the real WBXML codec, with only the mailbox gRPC client faked
public class ItemOperationsControllerTests
{
    private static ItemOperationsController CreateController(
        FakeMailboxStoreClient mailbox, out MemoryStream responseBody)
    {
        var attachments = new AttachmentService(mailbox, NullLogger<AttachmentService>.Instance);
        var controller = new ItemOperationsController(NullLogger<ItemOperationsController>.Instance, mailbox, attachments);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")])),
        };
        responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private static void SetRequest(ItemOperationsController controller, string fetchXml, bool acceptMultiPart = false)
    {
        var doc = new XmlDocument();
        doc.LoadXml($"""
            <?xml version="1.0" encoding="utf-8"?>
            <ItemOperations xmlns="ItemOperations" xmlns:airsync="AirSync" xmlns:airsyncbase="AirSyncBase">
              {fetchXml}
            </ItemOperations>
            """);

        controller.HttpContext.Items[ActiveSyncMiddleware.ContextKey] = new ActiveSyncContext
        {
            Command = EasCommand.ItemOperations,
            XmlDocument = doc,
            AcceptMultiPart = acceptMultiPart,
        };
    }

    // PartsCount(u32) + PartMetaData[count] (offset,length u32 each) + concatenated part bytes
    private static List<byte[]> DecodeMultiPart(byte[] body)
    {
        var count = BitConverter.ToInt32(body, 0);
        var parts = new List<byte[]>(count);
        for (int i = 0; i < count; i++)
        {
            var offset = BitConverter.ToInt32(body, 4 + i * 8);
            var length = BitConverter.ToInt32(body, 4 + i * 8 + 4);
            parts.Add(body[offset..(offset + length)]);
        }
        return parts;
    }

    private static XmlElement DecodeResponse(MemoryStream responseBody)
    {
        var decoder = new ASWBXML();
        decoder.LoadBytes(responseBody.ToArray());
        return decoder.GetXmlDocument().DocumentElement!;
    }

    private static XmlElement? Child(XmlElement root, string localName) =>
        root.ChildNodes.OfType<XmlElement>().FirstOrDefault(e => e.LocalName == localName);

    private static string? Value(XmlElement root, string localName) => Child(root, localName)?.InnerText;

    [Fact]
    public async Task FileReference_fetch_happy_path_returns_status_1_and_base64_data()
    {
        var expectedBytes = Encoding.UTF8.GetBytes("hello world");
        var mailbox = new FakeMailboxStoreClient
        {
            OnGetAttachmentContent = req => new AttachmentContent
            {
                Id = req.AttachmentId,
                DisplayName = "photo.jpg",
                ContentType = "image/jpeg",
                Content = ByteString.CopyFrom(expectedBytes),
            },
        };
        var controller = CreateController(mailbox, out var responseBody);
        SetRequest(controller, """
            <Fetch>
              <Store>Mailbox</Store>
              <airsyncbase:FileReference>att-1</airsyncbase:FileReference>
            </Fetch>
            """);

        await controller.Post(CancellationToken.None);

        var fetch = Child(Child(DecodeResponse(responseBody), "Response")!, "Fetch")!;
        Assert.Equal("1", Value(fetch, "Status"));
        Assert.Equal("att-1", Value(fetch, "FileReference"));

        var props = Child(fetch, "Properties")!;
        Assert.Equal(Convert.ToBase64String(expectedBytes), Value(props, "Data"));
        Assert.Equal($"0-{expectedBytes.Length - 1}", Value(props, "Range"));
        Assert.Equal(expectedBytes.Length.ToString(), Value(props, "Total"));
    }

    [Fact]
    public async Task FileReference_fetch_with_AcceptMultiPart_returns_multipart_response_with_raw_bytes()
    {
        var expectedBytes = Encoding.UTF8.GetBytes("hello world, this is not base64 on the wire");
        var mailbox = new FakeMailboxStoreClient
        {
            OnGetAttachmentContent = req => new AttachmentContent
            {
                Id = req.AttachmentId,
                ContentType = "image/jpeg",
                Content = ByteString.CopyFrom(expectedBytes),
            },
        };
        var controller = CreateController(mailbox, out var responseBody);
        SetRequest(controller, """
            <Fetch>
              <Store>Mailbox</Store>
              <airsyncbase:FileReference>att-1</airsyncbase:FileReference>
            </Fetch>
            """, acceptMultiPart: true);

        await controller.Post(CancellationToken.None);

        Assert.Equal("application/vnd.ms-sync.multipart", controller.HttpContext.Response.ContentType);

        var parts = DecodeMultiPart(responseBody.ToArray());
        Assert.Equal(2, parts.Count); // part 0 = WBXML, part 1 = raw attachment bytes

        var decoder = new ASWBXML();
        decoder.LoadBytes(parts[0]);
        var fetch = Child(Child(decoder.GetXmlDocument().DocumentElement!, "Response")!, "Fetch")!;
        Assert.Equal("1", Value(fetch, "Status"));

        var props = Child(fetch, "Properties")!;
        Assert.Null(Child(props, "Data")); // must not also inline it
        Assert.Equal("1", Value(props, "Part"));

        // part 1 is the exact original bytes, not base64 text
        Assert.Equal(expectedBytes, parts[1]);
    }

    [Fact]
    public async Task FileReference_fetch_with_AcceptMultiPart_large_attachment_round_trips()
    {
        // matches the real-world repro scale (~500KB JPEG) that WP7 rejected
        var expectedBytes = new byte[508_655];
        new Random(42).NextBytes(expectedBytes);

        var mailbox = new FakeMailboxStoreClient
        {
            OnGetAttachmentContent = req => new AttachmentContent
            {
                Id = req.AttachmentId,
                ContentType = "image/pjpeg",
                Content = ByteString.CopyFrom(expectedBytes),
            },
        };
        var controller = CreateController(mailbox, out var responseBody);
        SetRequest(controller, """
            <Fetch>
              <Store>Mailbox</Store>
              <airsyncbase:FileReference>0815eab1220e44bf925e739ae2856794</airsyncbase:FileReference>
            </Fetch>
            """, acceptMultiPart: true);

        await controller.Post(CancellationToken.None);

        Assert.Equal("application/vnd.ms-sync.multipart", controller.HttpContext.Response.ContentType);
        Assert.Equal(responseBody.Length, controller.HttpContext.Response.ContentLength);

        var raw = responseBody.ToArray();
        var parts = DecodeMultiPart(raw);
        Assert.Equal(2, parts.Count);

        var decoder = new ASWBXML();
        decoder.LoadBytes(parts[0]);
        var fetch = Child(Child(decoder.GetXmlDocument().DocumentElement!, "Response")!, "Fetch")!;
        Assert.Equal("1", Value(fetch, "Status"));

        var props = Child(fetch, "Properties")!;
        Assert.Null(Child(props, "Data"));
        Assert.Equal("1", Value(props, "Part"));
        Assert.Equal($"0-{expectedBytes.Length - 1}", Value(props, "Range"));
        Assert.Equal(expectedBytes.Length.ToString(), Value(props, "Total"));

        Assert.Equal(expectedBytes, parts[1]);

        // whole body must be exactly header + part0 + part1, no slack/misalignment
        Assert.Equal(4 + 2 * 8 + parts[0].Length + parts[1].Length, raw.Length);
    }

    [Fact]
    public async Task FileReference_fetch_for_missing_attachment_returns_status_15()
    {
        var mailbox = new FakeMailboxStoreClient
        {
            OnGetAttachmentContent = _ => throw new RpcException(new Status(StatusCode.NotFound, "not found")),
        };
        var controller = CreateController(mailbox, out var responseBody);
        SetRequest(controller, """
            <Fetch>
              <Store>Mailbox</Store>
              <airsyncbase:FileReference>does-not-exist</airsyncbase:FileReference>
            </Fetch>
            """);

        await controller.Post(CancellationToken.None);

        var fetch = Child(Child(DecodeResponse(responseBody), "Response")!, "Fetch")!;
        Assert.Equal("15", Value(fetch, "Status"));
        Assert.Null(Child(fetch, "Properties"));
    }

    [Fact]
    public async Task Fetch_with_non_Mailbox_Store_returns_status_9()
    {
        var mailbox = new FakeMailboxStoreClient();
        var controller = CreateController(mailbox, out var responseBody);
        SetRequest(controller, """
            <Fetch>
              <Store>GAL</Store>
              <airsync:ServerId>srv1</airsync:ServerId>
            </Fetch>
            """);

        await controller.Post(CancellationToken.None);

        var fetch = Child(Child(DecodeResponse(responseBody), "Response")!, "Fetch")!;
        Assert.Equal("9", Value(fetch, "Status"));
    }

    [Fact]
    public async Task Fetch_with_both_ServerId_and_FileReference_returns_status_2()
    {
        var mailbox = new FakeMailboxStoreClient();
        var controller = CreateController(mailbox, out var responseBody);
        SetRequest(controller, """
            <Fetch>
              <Store>Mailbox</Store>
              <airsync:ServerId>srv1</airsync:ServerId>
              <airsyncbase:FileReference>att-1</airsyncbase:FileReference>
            </Fetch>
            """);

        await controller.Post(CancellationToken.None);

        var fetch = Child(Child(DecodeResponse(responseBody), "Response")!, "Fetch")!;
        Assert.Equal("2", Value(fetch, "Status"));
    }

    [Fact]
    public async Task EmptyFolderContents_with_non_Mailbox_Store_returns_status_9()
    {
        var mailbox = new FakeMailboxStoreClient();
        var controller = CreateController(mailbox, out var responseBody);
        SetRequest(controller, """
            <EmptyFolderContents>
              <Store>GAL</Store>
              <airsync:CollectionId>1</airsync:CollectionId>
            </EmptyFolderContents>
            """);

        await controller.Post(CancellationToken.None);

        var empty = Child(Child(DecodeResponse(responseBody), "Response")!, "EmptyFolderContents")!;
        Assert.Equal("9", Value(empty, "Status"));
    }

    [Fact]
    public async Task Move_without_MoveAlways_returns_status_155_without_calling_backend()
    {
        // no OnMoveConversation configured; if the controller called the backend this
        // would throw, proving the MoveAlways-absent check is validated up front
        var mailbox = new FakeMailboxStoreClient();
        var controller = CreateController(mailbox, out var responseBody);
        SetRequest(controller, """
            <Move>
              <ConversationId>aGVsbG8=</ConversationId>
              <DstFldId>2</DstFldId>
            </Move>
            """);

        await controller.Post(CancellationToken.None);

        var move = Child(Child(DecodeResponse(responseBody), "Response")!, "Move")!;
        Assert.Equal("155", Value(move, "Status"));
    }

    [Fact]
    public async Task Move_with_MoveAlways_succeeds()
    {
        MoveConversationRequest? captured = null;
        var mailbox = new FakeMailboxStoreClient
        {
            OnMoveConversation = req =>
            {
                captured = req;
                return new MoveConversationResult { Status = MoveItemStatus.MoveSuccess, ItemsMoved = 3 };
            },
        };
        var controller = CreateController(mailbox, out var responseBody);
        SetRequest(controller, """
            <Move>
              <ConversationId>aGVsbG8=</ConversationId>
              <DstFldId>2</DstFldId>
              <Options>
                <MoveAlways/>
              </Options>
            </Move>
            """);

        await controller.Post(CancellationToken.None);

        var move = Child(Child(DecodeResponse(responseBody), "Response")!, "Move")!;
        Assert.Equal("1", Value(move, "Status"));
        Assert.NotNull(captured);
        Assert.Equal("2", captured!.DstCollectionId);
        Assert.Equal("hello", Encoding.UTF8.GetString(captured.ConversationId.ToByteArray()));
    }

    [Theory]
    [InlineData(MoveItemStatus.MoveInvalidSource, "6")]
    [InlineData(MoveItemStatus.MoveInvalidDest, "6")]
    [InlineData(MoveItemStatus.MoveSameCollection, "1")]
    [InlineData(MoveItemStatus.MoveInvalidClass, "2")]
    public async Task Move_failure_statuses_map_to_expected_ItemOperations_status(MoveItemStatus grpcStatus, string expected)
    {
        var mailbox = new FakeMailboxStoreClient
        {
            OnMoveConversation = _ => new MoveConversationResult { Status = grpcStatus },
        };
        var controller = CreateController(mailbox, out var responseBody);
        SetRequest(controller, """
            <Move>
              <ConversationId>aGVsbG8=</ConversationId>
              <DstFldId>2</DstFldId>
              <Options>
                <MoveAlways/>
              </Options>
            </Move>
            """);

        await controller.Post(CancellationToken.None);

        var move = Child(Child(DecodeResponse(responseBody), "Response")!, "Move")!;
        Assert.Equal(expected, Value(move, "Status"));
    }

    [Fact]
    public async Task Move_wraps_unexpected_backend_exceptions_as_status_3()
    {
        var mailbox = new FakeMailboxStoreClient
        {
            OnMoveConversation = _ => throw new RpcException(new Status(StatusCode.Unavailable, "down")),
        };
        var controller = CreateController(mailbox, out var responseBody);
        SetRequest(controller, """
            <Move>
              <ConversationId>aGVsbG8=</ConversationId>
              <DstFldId>2</DstFldId>
              <Options>
                <MoveAlways/>
              </Options>
            </Move>
            """);

        await controller.Post(CancellationToken.None);

        var move = Child(Child(DecodeResponse(responseBody), "Response")!, "Move")!;
        Assert.Equal("3", Value(move, "Status"));
    }
}
