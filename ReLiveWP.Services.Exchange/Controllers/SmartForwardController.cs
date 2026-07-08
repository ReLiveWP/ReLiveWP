using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[EasCommand(EasCommand.SmartForward)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
public class SmartForwardController(
    ILogger<SmartForwardController> logger,
    User.UserClient userClient,
    OutboundMailService outbound) : ActiveSyncCommandController
{
    [HttpPost]
    public async Task Post(CancellationToken ct)
    {
        var request = EasContext.XmlDocument is not null
            ? DeserializeRequest<SmartForwardRequest>(EasContext.XmlDocument)
            : null;

        if (request?.Mime is null)
        {
            await WriteWbxmlResponseAsync(new SmartForwardResponse { Status = 150 }, logger);
            return;
        }

        var userId = User.Id()!;
        var info = await userClient.GetUserInfoAsync(new GetUserInfoRequest { UserId = userId }, cancellationToken: ct);

        await outbound.SendAsync(userId, info.EmailAddress, request.Mime, request.SaveInSentItems is not null, ct);
        await outbound.MarkSourceVerbAsync(userId, request.Source?.ItemId, OutboundMailService.VerbForward, ct);

        HttpContext.Response.StatusCode = 200;
    }
}
