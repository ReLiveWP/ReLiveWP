using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[EasCommand(EasCommand.Sync)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
[Produces("application/vnd.ms-sync.wbxml")]
public class SyncController : ActiveSyncCommandController
{
    private readonly ILogger<SyncController> _logger;
    private readonly ItemSyncService _itemSync;

    public SyncController(ILogger<SyncController> logger, ItemSyncService itemSync)
    {
        _logger = logger;
        _itemSync = itemSync;
    }

    [HttpPost]
    public async Task Post(CancellationToken ct)
    {
        var request = EasContext.XmlDocument is not null
            ? DeserializeRequest<Sync>(EasContext.XmlDocument)
            : null;

        if (request?.Collections is null)
        {
            await WriteWbxmlResponseAsync(new Sync(), _logger);
            return;
        }

        var userId = User.Id()!;
        var collections = new List<SyncCollection>();

        foreach (var c in request.Collections.Items)
            collections.Add(await _itemSync.SyncAsync(userId, EasContext.DeviceId, c, ct));

        await WriteWbxmlResponseAsync(
            new Sync { Collections = new SyncCollections { Items = collections } }, _logger);
    }
}
