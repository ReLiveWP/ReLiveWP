using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Services.Activity.Models.Atom;
using ReLiveWP.Services.Activity.Services;

namespace ReLiveWP.Services.Activity.Controllers;

[Controller]
[Route("/Users({id}/[action]")]
[Route("/Users({provider}:{id}/[action]")]
[Produces("application/atom+xml")]
public class UsersController(ActivityProviderService activityProvider) : Controller
{
    [HttpPost]
    [Authorize]
    [Route("/Users({id})/Status")] // TODO: Move
    public async Task<ActionResult> Status(long id, [FromBody] LiveEntry entry)
    {
        Response.Headers.Append("X-QueriedServices", "WL");
        var provider = await activityProvider.GetActivityProviderAsync();
        if (provider == null)
            return NoContent();

        // todo: attachments, etc.
        await provider.CreatePostAsync(entry.Title.Value);

        return NoContent();
    }

    [ActionName("contacts_activities_route_for_user")]
    public async Task<ActionResult> ContactsActivities(
        string id,
        string? provider = null,
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Source")] string source = "WL",
        [FromQuery(Name = "Type")] string type = "all",
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "$xslt")] string? xslt = null)
    {
        return NoContent();
    }


    [ActionName("activities_route_for_user")]
    public async Task<ActionResult> Activities(
        string id,
        string? provider = null,
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Source")] string source = "WL",
        [FromQuery(Name = "Type")] string type = "all",
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "$xslt")] string? xslt = null)
    {
        return NoContent();
    }
}
