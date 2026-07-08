using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[EasCommand(EasCommand.SendMail)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
public class SendMailController(
    ILogger<SendMailController> logger,
    User.UserClient userClient,
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
            // 150 = invalid MIME / message could not be sent.
            await WriteWbxmlResponseAsync(new SendMailResponse { Status = 150 }, logger);
            return;
        }

        var userId = User.Id()!;
        var fromAddress = await GetPrimaryAddressAsync(userId, ct);

        await outbound.SendAsync(userId, fromAddress, request.Mime, request.SaveInSentItems is not null, ct);

        // Success: empty HTTP 200 with no body (MS-ASCMD §2.2.1.17).
        HttpContext.Response.StatusCode = 200;
    }

    private async Task<string> GetPrimaryAddressAsync(string userId, CancellationToken ct)
    {
        var info = await userClient.GetUserInfoAsync(new GetUserInfoRequest { UserId = userId }, cancellationToken: ct);
        return info.EmailAddress;
    }
}
