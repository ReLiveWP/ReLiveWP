using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[EasCommand(EasCommand.SmartForward)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
public class SmartForwardController(
    ILogger<SmartForwardController> logger,
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
            await WriteWbxmlResponseAsync(new SmartForwardResponse { Status = EasStatus.MailSubmissionFailed }, logger);
            return;
        }

        var userId = User.Id()!;
        var status = await outbound.SubmitAsync(
            userId, request.Mime, request.SaveInSentItems is not null, request.ClientId, ct);

        if (status != EasStatus.Success)
        {
            await WriteWbxmlResponseAsync(new SmartForwardResponse { Status = status }, logger);
            return;
        }

        await outbound.MarkSourceVerbAsync(userId, request.Source?.ItemId, OutboundMailService.VerbForward, ct);

        HttpContext.Response.StatusCode = 200;
    }
}
