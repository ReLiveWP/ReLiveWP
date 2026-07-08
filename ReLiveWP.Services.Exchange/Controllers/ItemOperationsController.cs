using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Extensions;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[EasCommand(EasCommand.ItemOperations)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
[Produces("application/vnd.ms-sync.wbxml")]
public class ItemOperationsController(
    ILogger<ItemOperationsController> logger,
    MailboxStore.MailboxStoreClient mailbox) : ActiveSyncCommandController
{
    [HttpPost]
    public async Task Post(CancellationToken ct)
    {
        var request = EasContext.XmlDocument is not null
            ? DeserializeRequest<ItemOperations>(EasContext.XmlDocument)
            : null;

        var response = new ItemOperationsResponse { Status = 1, Response = new ItemOperationsResponseBody() };

        if (request is not null)
        {
            var userId = User.Id()!;
            foreach (var fetch in request.Fetch)
                response.Response.Fetch.Add(await FetchAsync(userId, fetch, ct));
        }

        await WriteWbxmlResponseAsync(response, logger);
    }

    private async Task<ItemOperationsFetchResponse> FetchAsync(string userId, ItemOperationsFetch fetch, CancellationToken ct)
    {
        var result = new ItemOperationsFetchResponse
        {
            Store = fetch.Store,
            CollectionId = fetch.CollectionId,
            ServerId = fetch.ServerId,
        };

        if (string.IsNullOrEmpty(fetch.ServerId))
        {
            result.Status = 2; // protocol error / unsupported (only Mailbox ServerId fetch implemented)
            return result;
        }

        Item item;
        try
        {
            item = await mailbox.GetItemAsync(
                new GetItemRequest { UserId = userId, ServerId = fetch.ServerId }, cancellationToken: ct);
        }
        catch (RpcException e) when (e.StatusCode == global::Grpc.Core.StatusCode.NotFound)
        {
            result.Status = 6; // item not found / conversion error
            return result;
        }

        // ItemOperations returns the full body; honour an explicit BodyPreference if supplied.
        var bodyPref = fetch.Options?.BodyPreference is { Count: > 0 } prefs
            ? prefs.FirstOrDefault(p => p.Type == BodyType.HTML) ?? prefs[0]
            : null;

        var (appData, itemClass) = item.BodyCase switch
        {
            Item.BodyOneofCase.Email => (item.Email.ToApplicationData(bodyPref), "Email"),
            Item.BodyOneofCase.Contact => (item.Contact.ToApplicationData(), "Contacts"),
            Item.BodyOneofCase.Calendar => (item.Calendar.ToApplicationData(), "Calendar"),
            _ => (null, null),
        };

        if (appData is null)
        {
            result.Status = 6;
            return result;
        }

        result.Class = itemClass;
        result.Properties = new ItemOperationsProperties { Elements = appData.Elements };
        return result;
    }
}
