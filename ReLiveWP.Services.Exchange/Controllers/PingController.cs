using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[EasCommand(EasCommand.Ping)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
[Produces("application/vnd.ms-sync.wbxml")]
public class PingController(ILogger<PingController> logger, PingService pingService) : ActiveSyncCommandController
{
    [HttpPost]
    public async Task Post()
    {
        var request = EasContext.XmlDocument is not null
            ? DeserializeRequest<Ping>(EasContext.XmlDocument)
            : null;

        var userId = User.Id()!;

        var response = await pingService.PingAsync(
            userId, EasContext.DeviceId, request, HttpContext.RequestAborted);

        await WriteWbxmlResponseAsync(response, logger);
    }
}
