using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[EasCommand(EasCommand.FolderCreate)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
[Produces("application/vnd.ms-sync.wbxml")]
public class FolderCreateController(ILogger<FolderCreateController> logger,
                                    FolderSyncService folderSync) : ActiveSyncCommandController
{
    [HttpPost]
    public async Task Post(CancellationToken ct)
    {
        var request = EasContext.XmlDocument is not null
            ? DeserializeRequest<FolderCreate>(EasContext.XmlDocument)
            : null;

        var userId = User.Id()!;

        logger.LogInformation(
            "FolderCreate from {User} on {DeviceId} ({DeviceType}), SyncKey={SyncKey}, Name={Name}, Type={Type}",
            userId, EasContext.DeviceId, EasContext.DeviceType, request?.SyncKey, request?.DisplayName, request?.Type);

        var response = await folderSync.CreateAsync(userId, EasContext.DeviceId, request, ct);

        await WriteWbxmlResponseAsync(response, logger);
    }
}
