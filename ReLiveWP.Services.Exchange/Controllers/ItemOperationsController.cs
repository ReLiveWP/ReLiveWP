using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Extensions;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[EasCommand(EasCommand.ItemOperations)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
[Produces("application/vnd.ms-sync.wbxml")]
public class ItemOperationsController(
    ILogger<ItemOperationsController> logger,
    MailboxStore.MailboxStoreClient mailbox,
    AttachmentService attachments) : ActiveSyncCommandController
{
    // we only have Mailbox right now
    private static bool IsUnsupportedStore(string? store) =>
        !string.IsNullOrEmpty(store) && !string.Equals(store, "Mailbox", StringComparison.OrdinalIgnoreCase);

    [HttpPost]
    public async Task Post(CancellationToken ct)
    {
        var request = EasContext.XmlDocument is not null
            ? DeserializeRequest<ItemOperations>(EasContext.XmlDocument)
            : null;

        var response = new ItemOperationsResponse { Status = 1, Response = new ItemOperationsResponseBody() };

        // raw (non-base64) bytes for clients that sent MS-ASAcceptMultiPart; part 0 is always
        // the WBXML itself, so a Fetch response referencing part N here means index N-1
        var multipartParts = new List<byte[]>();

        if (request is not null)
        {
            var userId = User.Id()!;
            foreach (var fetch in request.Fetch)
                response.Response.Fetch.Add(await FetchAsync(userId, fetch, multipartParts, ct));
            foreach (var empty in request.EmptyFolderContents)
                response.Response.EmptyFolderContents.Add(await EmptyFolderContentsAsync(userId, empty, ct));
            foreach (var move in request.Move)
                response.Response.Move.Add(await MoveAsync(userId, move, ct));
        }

        if (multipartParts.Count > 0)
            await WriteMultiPartResponseAsync(response, multipartParts, logger);
        else
            await WriteWbxmlResponseAsync(response, logger);
    }

    private async Task<ItemOperationsFetchResponse> FetchAsync(
        string userId, ItemOperationsFetch fetch, List<byte[]> multipartParts, CancellationToken ct)
    {
        var result = new ItemOperationsFetchResponse
        {
            CollectionId = fetch.CollectionId,
            ServerId = fetch.ServerId,
        };

        if (IsUnsupportedStore(fetch.Store))
        {
            result.Status = 9; // store unknown/unsupported (e.g. GAL, DocumentLibrary)
            return result;
        }

        var hasServerId = !string.IsNullOrEmpty(fetch.ServerId);
        var hasFileReference = !string.IsNullOrEmpty(fetch.FileReference);

        // exactly one location per Fetch
        if (hasServerId == hasFileReference)
        {
            result.Status = 2;
            return result;
        }

        try
        {
            return hasFileReference
                ? await FetchAttachmentAsync(userId, fetch, result, multipartParts, ct)
                : await FetchPimAsync(userId, fetch, result, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unexpected failure during ItemOperations Fetch for {User}", userId);
            result.Status = 3;
            return result;
        }
    }

    private async Task<ItemOperationsFetchResponse> FetchAttachmentAsync(
        string userId, ItemOperationsFetch fetch, ItemOperationsFetchResponse result,
        List<byte[]> multipartParts, CancellationToken ct)
    {
        result.FileReference = fetch.FileReference;

        var resolved = await attachments.ResolveAsync(userId, fetch.FileReference, fetch.Options?.Range, ct);
        result.Status = resolved.Status switch
        {
            AttachmentResolveStatus.Success => 1,
            AttachmentResolveStatus.NotFound => 15,     // attachment/id invalid (MS-ASAIRS 2.2.2.24.1)
            AttachmentResolveStatus.InvalidRange => 8,  // byte-range invalid or too large
            AttachmentResolveStatus.Empty => 10,        // file is empty
            AttachmentResolveStatus.IoFailure => 12,    // I/O failure fetching the content
            _ => 3,
        };

        if (resolved is { Status: AttachmentResolveStatus.Success, Data: { } data })
        {
            result.Properties = new ItemOperationsProperties
            {
                Range = data.RangeHeader,
                Total = data.TotalSize,
                ContentType = data.ContentType,
            };

            if (EasContext.AcceptMultiPart)
            {
                multipartParts.Add(data.Content);
                result.Properties.Part = multipartParts.Count; // 1-based: part 0 is the WBXML
            }
            else
            {
                result.Properties.Data = Convert.ToBase64String(data.Content);
            }
        }

        return result;
    }

    private async Task<ItemOperationsFetchResponse> FetchPimAsync(
        string userId, ItemOperationsFetch fetch, ItemOperationsFetchResponse result, CancellationToken ct)
    {
        if (fetch.Options?.Range is not null)
            logger.LogDebug("Ignoring Range on PIM Fetch for ServerId {ServerId}", fetch.ServerId);

        Item item;
        try
        {
            item = await mailbox.GetItemAsync(
                new GetItemRequest { UserId = userId, ServerId = fetch.ServerId! }, cancellationToken: ct);
        }
        catch (RpcException e) when (e.StatusCode == global::Grpc.Core.StatusCode.NotFound)
        {
            result.Status = 14; // Mailbox fetch provider: item not found / failed conversion
            return result;
        }
        catch (RpcException e)
        {
            logger.LogWarning(e, "GetItem RPC failure during Fetch for {ServerId}", fetch.ServerId);
            result.Status = 12;
            return result;
        }

        // pass the client's own preferences through rather than substituting a bare MIME one -
        // the substitute dropped TruncationSize/AllOrNone and always returned the whole blob
        List<BodyPreference>? bodyPrefs =
            fetch.Options?.BodyPreference is { Count: > 0 } p ? [.. p] : null;

        // MIMESupport asks for MIME without necessarily offering a BodyPreference for it
        if (fetch.Options?.MIMESupport > 0 && bodyPrefs?.Any(x => x.Type == BodyType.MIME) != true)
        {
            bodyPrefs ??= [];
            bodyPrefs.Insert(0, new BodyPreference { Type = BodyType.MIME });
        }

        var (appData, itemClass) = item.BodyCase switch
        {
            Item.BodyOneofCase.Email => (item.Email.ToApplicationData(bodyPrefs), "Email"),
            Item.BodyOneofCase.Contact => (item.Contact.ToApplicationData(), "Contacts"),
            Item.BodyOneofCase.Calendar => (item.Calendar.ToApplicationData(), "Calendar"),
            Item.BodyOneofCase.Task => (item.Task.ToApplicationData(), "Tasks"),
            _ => (null, null),
        };

        if (appData is null)
        {
            result.Status = 14; // Mailbox fetch provider: item failed conversion
            return result;
        }

        result.Class = itemClass;
        result.Properties = new ItemOperationsProperties { Elements = appData.Elements };
        return result;
    }

    private async Task<ItemOperationsEmptyFolderResponse> EmptyFolderContentsAsync(
        string userId, ItemOperationsEmptyFolder empty, CancellationToken ct)
    {
        var result = new ItemOperationsEmptyFolderResponse
        {
            CollectionId = empty.CollectionId,
        };

        if (IsUnsupportedStore(empty.Store))
        {
            result.Status = 9; // store unknown/unsupported
            return result;
        }

        if (string.IsNullOrEmpty(empty.CollectionId))
        {
            result.Status = 2; // CollectionId is required
            return result;
        }

        try
        {
            var deleteSubFolders = empty.Options?.DeleteSubFolders == 1;
            var reply = await mailbox.EmptyFolderAsync(new EmptyFolderRequest
            {
                UserId = userId,
                CollectionId = empty.CollectionId,
                DeleteSubFolders = deleteSubFolders,
            }, cancellationToken: ct);

            logger.LogInformation("ItemOperations EmptyFolderContents {Collection} (subfolders={Sub}) for {User}: found={Found} deleted={Deleted}",
                empty.CollectionId, deleteSubFolders, userId, reply.Found, reply.ItemsDeleted);

            result.Status = reply.Found ? 1 : 6; // 6 = document/item not found family
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unexpected failure during ItemOperations EmptyFolderContents for {Collection}", empty.CollectionId);
            result.Status = 3;
        }

        return result;
    }

    private async Task<ItemOperationsMoveResponse> MoveAsync(string userId, ItemOperationsMove move, CancellationToken ct)
    {
        var result = new ItemOperationsMoveResponse { ConversationId = move.ConversationId };

        if (move.Options?.MoveAlways is null)
        {
            result.Status = 155;
            return result;
        }

        if (move.ConversationId is not { Length: > 0 } || string.IsNullOrEmpty(move.DstFldId))
        {
            result.Status = 2; // protocol error, missing required field
            return result;
        }

        try
        {
            var reply = await mailbox.MoveConversationAsync(new MoveConversationRequest
            {
                UserId = userId,
                ConversationId = Google.Protobuf.ByteString.CopyFrom(move.ConversationId),
                DstCollectionId = move.DstFldId,
            }, cancellationToken: ct);

            logger.LogInformation("ItemOperations Move conversation to {Dst} for {User}: status={Status} moved={Moved}",
                move.DstFldId, userId, reply.Status, reply.ItemsMoved);

            // spec status 156 (destination must be IPF.Note) is intentionally never produced,
            // conversations here are email-only by construction (see MoveConversation's proto comment)
            result.Status = reply.Status switch
            {
                MoveItemStatus.MoveSuccess => 1,
                MoveItemStatus.MoveSameCollection => 1, // already there; nothing to do, not an error
                MoveItemStatus.MoveInvalidSource => 6,  // conversation not found
                MoveItemStatus.MoveInvalidDest => 6,    // destination folder not found
                MoveItemStatus.MoveInvalidClass => 2,   // shouldn't happen for email-only conversations
                _ => 3,
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unexpected failure during ItemOperations Move for {User}", userId);
            result.Status = 3;
        }

        return result;
    }
}
