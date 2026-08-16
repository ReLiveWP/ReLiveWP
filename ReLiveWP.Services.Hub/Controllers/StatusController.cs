using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Services.Grpc.ClearingHouse;
using ReLiveWP.Services.Hub.Models;
using ClearingHouseClient = ReLiveWP.Services.Grpc.ClearingHouse.ClearingHouse.ClearingHouseClient;

namespace ReLiveWP.Services.Hub.Controllers;

[ApiController]
[Authorize]
[Route("status")]
public class StatusController(ClearingHouseClient clearingHouse) : Controller
{
    [HttpGet]
    public async Task<ActionResult<SyncStatusResponse>> Get([FromQuery] string? connectionId)
    {
        var request = new GetSyncStatusRequest();
        if (!string.IsNullOrWhiteSpace(connectionId)) request.ConnectionId = connectionId;

        var result = await clearingHouse.GetSyncStatusAsync(request);

        return new SyncStatusResponse([.. result.Sources.Select(s => new SourceSyncStatusModel(
            s.ConnectionId, s.ServiceId, s.SourceId, s.DetachAfterRun,
            s.Running, s.Queued,
            s.HasLastSyncedAt ? s.LastSyncedAt : null,
            s.HasLastFailure ? s.LastFailure : null,
            s.ConsecutiveFailures,
            s.Created, s.Updated, s.Deleted, s.Skipped))]);
    }
}
