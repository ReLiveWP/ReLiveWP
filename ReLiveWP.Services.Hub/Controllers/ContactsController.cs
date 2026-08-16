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
    [HttpGet]
    [ActionName("sync")]
    public async Task<ActionResult<ContactSyncListResponse>> Get([FromQuery] string? connectionId)
    {
        var request = new GetContactSyncRequest();
        if (!string.IsNullOrWhiteSpace(connectionId)) request.ConnectionId = connectionId;

        var result = await clearingHouse.GetContactSyncAsync(request);

        return new ContactSyncListResponse([.. result.Connections.Select(Model)]);
    }

    [HttpPost]
    [ActionName("sync")]
    public async Task<ActionResult<ContactSyncModel>> SyncNow([FromBody] ContactSyncNowModel model) =>
        Model(await clearingHouse.SyncContactsNowAsync(new() { ConnectionId = model.ConnectionId }));

    [HttpPut]
    [ActionName("sync")]
    public async Task<ActionResult<ContactSyncModel>> SetEnabled([FromBody] SetContactSyncModel model) =>
        Model(await clearingHouse.SetContactSyncAsync(
            new() { ConnectionId = model.ConnectionId, Enabled = model.Enabled }));

    private static ContactSyncModel Model(ContactSyncStatus s) => new(
        s.ConnectionId, s.ServiceId, s.Enabled, s.Running, s.Queued,
        s.HasLastSyncedAt ? s.LastSyncedAt : null,
        s.HasLastFailure ? s.LastFailure : null,
        s.Created, s.Updated, s.Deleted, s.Skipped);
}
