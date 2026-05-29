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
                                  ProvisioningService provisioningService,
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

        await provisioningService.EnsureProvisionedAsync(userId, ct);

        // Extract annotation subscription before syncing (client sends Name-only entries)
        var requestedAnnotations = request?.Annotations?.RequestedNames();
        if (requestedAnnotations is { Count: > 0 })
            EasContext.FolderSyncAnnotations = requestedAnnotations;

        var response = await folderSync.SyncAsync(userId, EasContext.DeviceId, request?.SyncKey, ct);

        // Populate FolderSync-level annotation response.
        // SID identifies the user's own Live contact store — we use their PUID (CID).
        if (EasContext.FolderSyncAnnotations?.Contains("SID") == true && EasContext.Cid is { } cid)
        {
            response.Annotations = new Annotations
            {
                Items = [new Annotation { Name = "SID", Value = cid.ToString() }]
            };
        }

        await WriteWbxmlResponseAsync(response, logger);
    }
}
