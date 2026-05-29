using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[EasCommand(EasCommand.GetItemEstimate)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
[Produces("application/vnd.ms-sync.wbxml")]
public class GetItemEstimateController(
    ILogger<GetItemEstimateController> logger,
    GetItemEstimateService estimateService) : ActiveSyncCommandController
{
    [HttpPost]
    public async Task Post(CancellationToken ct)
    {
        var request = EasContext.XmlDocument is not null
            ? DeserializeRequest<GetItemEstimateRequest>(EasContext.XmlDocument)
            : null;

        var userId = User.Id()!;

        logger.LogInformation("GetItemEstimate from {User} on {DeviceId} ({DeviceType})",
            userId, EasContext.DeviceId, EasContext.DeviceType);

        var response = await estimateService.EstimateAsync(
            userId, EasContext.DeviceId, request ?? new(), ct);

        await WriteWbxmlResponseAsync(response, logger);
    }
}
