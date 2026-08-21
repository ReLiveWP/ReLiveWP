using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[EasCommand(EasCommand.SendMail)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
public class SendMailController(
    ILogger<SendMailController> logger,
    OutboundMailService outbound) : ActiveSyncCommandController
{
    [HttpPost]
    public async Task Post(CancellationToken ct)
    {
        var request = EasContext.XmlDocument is not null
            ? DeserializeRequest<SendMailRequest>(EasContext.XmlDocument)
            : null;

        if (request?.Mime is null)
        {
            await WriteWbxmlResponseAsync(new SendMailResponse { Status = EasStatus.MailSubmissionFailed }, logger);
            return;
        }

        var status = await outbound.SubmitAsync(
            User.Id()!, request.Mime, request.SaveInSentItems is not null, request.ClientId, ct);

        if (status != EasStatus.Success)
        {
            await WriteWbxmlResponseAsync(new SendMailResponse { Status = status }, logger);
            return;
        }

        HttpContext.Response.StatusCode = 200;
    }
}
