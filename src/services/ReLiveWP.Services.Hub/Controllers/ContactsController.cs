using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Services.Grpc.ClearingHouse;
using ReLiveWP.Services.Hub.Models;
using ClearingHouseClient = ReLiveWP.Services.Grpc.ClearingHouse.ClearingHouse.ClearingHouseClient;

namespace ReLiveWP.Services.Hub.Controllers;

[ApiController]
[Authorize]
[Route("contacts/[action]")]
public class ContactsController(ClearingHouseClient clearingHouse) : Controller
{
    private const SyncKind Kind = SyncKind.Contacts;

    [HttpGet]
    [ActionName("sync")]
    public async Task<ActionResult<SyncListResponse>> Get([FromQuery] string? connectionId)
    {
        var request = new GetSyncRequest { Kind = Kind };
        if (!string.IsNullOrWhiteSpace(connectionId)) request.ConnectionId = connectionId;

        var result = await clearingHouse.GetSyncAsync(request);

        return new SyncListResponse([.. result.Connections.Select(SyncModel.From)]);
    }

    [HttpPost]
    [ActionName("sync")]
    public async Task<ActionResult<SyncModel>> SyncNow([FromBody] SyncNowModel model) =>
        SyncModel.From(await clearingHouse.SyncNowAsync(
            new() { Kind = Kind, ConnectionId = model.ConnectionId }));

    [HttpPut]
    [ActionName("sync")]
    public async Task<ActionResult<SyncModel>> SetEnabled([FromBody] SetSyncModel model) =>
        SyncModel.From(await clearingHouse.SetSyncAsync(
            new() { Kind = Kind, ConnectionId = model.ConnectionId, Enabled = model.Enabled }));
}
