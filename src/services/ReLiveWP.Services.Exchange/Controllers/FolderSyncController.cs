using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[EasCommand(EasCommand.FolderSync)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
[Produces("application/vnd.ms-sync.wbxml")]
public class FolderSyncController(ILogger<FolderSyncController> logger,
                                  FolderSyncService folderSync) : ActiveSyncCommandController
{
    [HttpPost]
    public async Task Post(CancellationToken ct)
    {
        var request = EasContext.XmlDocument is not null
            ? DeserializeRequest<FolderSync>(EasContext.XmlDocument)
            : null;

        var userId = User.Id()!;

        logger.LogInformation(
            "FolderSync from {User} on {DeviceId} ({DeviceType}), SyncKey={SyncKey}",
            userId, EasContext.DeviceId, EasContext.DeviceType, request?.SyncKey ?? "0");

        var response = await folderSync.SyncAsync(userId, EasContext.DeviceId, request?.SyncKey,
            request?.Annotations?.RequestedNames(), ct);


        await WriteWbxmlResponseAsync(response, logger);
    }
}
