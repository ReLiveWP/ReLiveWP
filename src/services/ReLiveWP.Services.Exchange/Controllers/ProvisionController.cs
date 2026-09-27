using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[EasCommand(EasCommand.Provision)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
[Produces("application/vnd.ms-sync.wbxml")]
public class ProvisionController(ILogger<ProvisionController> logger) : ActiveSyncCommandController
{
    [HttpPost]
    public async Task Post()
    {
        var request = EasContext.XmlDocument is not null
            ? DeserializeRequest<ProvisionRequest>(EasContext.XmlDocument)
            : null;

        var userId = User.Id()!;
        var isAck = request?.Policies?.Policy?.PolicyKey is not null;

        logger.LogInformation("Provision ({Phase}) from {User} on {DeviceId}",
            isAck ? "ack" : "download", userId, EasContext.DeviceId);

        var response = isAck ? BuildAckResponse() : BuildDownloadResponse();
        await WriteWbxmlResponseAsync(response, logger);
    }

    private static ProvisionResponse BuildDownloadResponse() => new()
    {
        DeviceInformation = new ProvisionDeviceInformationResponse(),
        Policies = new ProvisionPolicies
        {
            Policy = new ProvisionPolicy
            {
                Status = "1",
                PolicyKey = ProvisionResponse.TempPolicyKey,
                Data = new ProvisionData { EasProvisionDoc = new EasProvisionDoc() },
            },
        },
    };

    private static ProvisionResponse BuildAckResponse() => new()
    {
        Policies = new ProvisionPolicies
        {
            Policy = new ProvisionPolicy
            {
                Status = "1",
                PolicyKey = ProvisionResponse.FinalPolicyKey,
            },
        },
    };
}
