using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[EasCommand(EasCommand.Settings)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
[Produces("application/vnd.ms-sync.wbxml")]
public class SettingsController(
    ILogger<SettingsController> logger,
    SettingsService settingsService) : ActiveSyncCommandController
{
    [HttpPost]
    public async Task Post(CancellationToken ct)
    {
        var request = EasContext.XmlDocument is not null
            ? DeserializeRequest<SettingsRequest>(EasContext.XmlDocument)
            : null;

        var userId = User.Id()!;

        logger.LogInformation("Settings from {User} on {DeviceId} ({DeviceType})",
            userId, EasContext.DeviceId, EasContext.DeviceType);

        var response = await settingsService.HandleAsync(
            userId, EasContext.DeviceId, request ?? new(), ct);

        await WriteWbxmlResponseAsync(response, logger);
    }
}
