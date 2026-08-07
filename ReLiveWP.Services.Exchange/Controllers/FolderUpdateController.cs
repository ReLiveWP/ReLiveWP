using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[EasCommand(EasCommand.FolderUpdate)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
[Produces("application/vnd.ms-sync.wbxml")]
public class FolderUpdateController(ILogger<FolderUpdateController> logger,
                                    FolderSyncService folderSync) : ActiveSyncCommandController
{
    [HttpPost]
    public async Task Post(CancellationToken ct)
    {
        var request = EasContext.XmlDocument is not null
            ? DeserializeRequest<FolderUpdate>(EasContext.XmlDocument)
            : null;

        var userId = User.Id()!;

        logger.LogInformation(
            "FolderUpdate from {User} on {DeviceId} ({DeviceType}), SyncKey={SyncKey}, ServerId={ServerId}, Name={Name}",
            userId, EasContext.DeviceId, EasContext.DeviceType, request?.SyncKey, request?.ServerId, request?.DisplayName);

        var response = await folderSync.UpdateAsync(userId, EasContext.DeviceId, request, ct);

        await WriteWbxmlResponseAsync(response, logger);
    }
}
