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
    [ActionName("sources")]
    public async Task<ActionResult<ContactSourcesResponse>> GetSources([FromQuery] string connectionId)
    {
        var response = await clearingHouse.ListContactSourcesAsync(new() { ConnectionId = connectionId });

        return new ContactSourcesResponse(response.ServiceId,
            [.. response.Sources.Select(s => new ContactSourceModel(
                s.Id, s.DisplayName, s.HasCount ? s.Count : null, s.IsDefault))]);
    }

    [HttpPost]
    [ActionName("import")]
    public async Task<ActionResult<ImportContactsResponse>> Import([FromBody] ImportContactsModel model)
    {
        var request = new ImportContactsRequest
        {
            ConnectionId = model.ConnectionId,
            KeepInSync = model.KeepInSync,
        };

        if (model.SourceIds is { Length: > 0 })
            request.SourceIds.AddRange(model.SourceIds);

        var result = await clearingHouse.ImportContactsAsync(request);

        return new ImportContactsResponse([.. result.QueuedSourceIds]);
    }
}
