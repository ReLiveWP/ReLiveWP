using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Activity.Models.Atom;
using ReLiveWP.Services.Activity.Services;
using Link = Atom.Xml.Link;

namespace ReLiveWP.Services.Activity.Controllers;

[Controller]
[Route("/Users({id})/[action]")]
[Route("/Users({provider}:{id})/[action]")]
[Produces("application/atom+xml")]
public class UsersController(
    FeedRenderer feeds,
    ActivityProviderService activityProvider) : Controller
{
    [HttpPost]
    [Authorize]
    [Route("/Users({id})/Status")] // TODO: Move
    public async Task<ActionResult> Status(long id, [FromBody] LiveEntry entry)
    {
        Response.Headers.Append("X-QueriedServices", "WL");
        var provider = await activityProvider.GetOwnedProviderAsync();
        if (provider == null)
            return NoContent();

        // todo: attachments, etc.
        await provider.CreatePostAsync(entry.Title.Value);

        return NoContent();
    }

    [Authorize]
    [Route("/Users({provider}:{id})/ContactsActivities", Name = "contacts_activities_route_for_user")]
    public Task<ActionResult<LiveFeed>> ContactsActivities(
        string id,
        string? provider = null,
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Source")] string source = "WL",
        [FromQuery(Name = "Type")] string type = "all",
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "$xslt")] string? xslt = null)
        // A contact card's "what's new" is that contact's own activity.
        => ContactFeedAsync(id, count, "contacts_activities_route_for_user");

    [Authorize]
    [Route("/Users({provider}:{id})/Activities", Name = "activities_route_for_user")]
    public Task<ActionResult<LiveFeed>> Activities(
        string id,
        string? provider = null,
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Source")] string source = "WL",
        [FromQuery(Name = "Type")] string type = "all",
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "$xslt")] string? xslt = null)
        => ContactFeedAsync(id, count, "activities_route_for_user");

    private async Task<ActionResult<LiveFeed>> ContactFeedAsync(string id, int count, string routeName)
    {
        Response.Headers.Append("X-QueriedServices", "WL");

        if (!long.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cid))
            return NoContent();

        var contactAuthor = new LiveAuthor()
        {
            Id = cid.ToString(CultureInfo.InvariantCulture),
            Name = "",
            Url = this.Url.Link(routeName, new { provider = "WL", id = cid.ToString(CultureInfo.InvariantCulture) }),
            Links = []
        };

        var feed = new LiveFeed()
        {
            Title = "What's New",
            Id = this.Url.Link(routeName, new { provider = "WL", id = cid.ToString(CultureInfo.InvariantCulture) }),
            Updated = DateTime.UtcNow,
            Author = contactAuthor,
            Links = [new Link(this.Url.Link(routeName, new { provider = "WL", id = cid.ToString(CultureInfo.InvariantCulture) }))]
        };

        var sources = await activityProvider.GetContactFeedSourcesAsync(cid, User.Id()!, HttpContext.RequestAborted);
        feed.Entries.AddRange(
            await feeds.RenderContactFeedAsync(Url, activityProvider.PublicProviders, sources, cid, count));

        return feed;
    }
}
