using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[EasCommand(EasCommand.MoveItems)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
[Produces("application/vnd.ms-sync.wbxml")]
public class MoveItemsController(
    ILogger<MoveItemsController> logger,
    MailboxStore.MailboxStoreClient mailbox) : ActiveSyncCommandController
{
    [HttpPost]
    public async Task Post(CancellationToken ct)
    {
        var request = EasContext.XmlDocument is not null
            ? DeserializeRequest<MoveItems>(EasContext.XmlDocument)
            : null;

        var userId = User.Id()!;
        var response = new MoveItemsResponse();

        if (request is not null)
        {
            foreach (var move in request.Moves)
            {
                logger.LogInformation("MoveItems {ServerId} from {Src} to {Dst} for {User}",
                    move.SrcMsgId, move.SrcFldId, move.DstFldId, userId);

                var result = await mailbox.MoveItemAsync(new MoveItemRequest
                {
                    UserId = userId,
                    ServerId = move.SrcMsgId,
                    DstCollectionId = move.DstFldId,
                    SrcCollectionId = move.SrcFldId,
                }, cancellationToken: ct);

                var status = result.Status switch
                {
                    MoveItemStatus.MoveSuccess => 3,
                    MoveItemStatus.MoveInvalidSource => 1,
                    MoveItemStatus.MoveInvalidDest => 2,
                    MoveItemStatus.MoveSameCollection => 4,
                    // "invalid destination collection ID" seems good enough
                    MoveItemStatus.MoveInvalidClass => 2,
                    _ => 1,
                };

                response.Responses.Add(new MoveResponse
                {
                    SrcMsgId = move.SrcMsgId,
                    Status = status,
                    DstMsgId = result.Status == MoveItemStatus.MoveSuccess ? result.NewServerId : null,
                });
            }
        }

        await WriteWbxmlResponseAsync(response, logger);
    }
}
