using System.Globalization;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Activity.Models.Atom;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Grpc;
using Link = Atom.Xml.Link;

namespace ReLiveWP.Services.Activity.Controllers;

public class Identifiers
{
    [XmlElement("Identifier")]
    public List<Identifier> IdentifierList { get; set; } = [];
}

public class Identifier
{
    [XmlElement]
    public string SourceId { get; set; } = default!;
    [XmlElement]
    public string ObjectId { get; set; } = default!;
}

[Authorize]
[Controller]
[Produces("application/atom+xml")]
public class ActivitiesController(
    ILogger<ActivitiesController> logger,
    User.UserClient userClient,
    FeedRenderer feeds,
    ActivityProviderService activityProvider) : Controller
{
    [HttpPost]
    [Route("/Activities", Name = "activities_route")]
    public async Task<ActionResult<LiveFeed>> Activities(
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Type")] string type = "all",
        [FromQuery(Name = "$xslt")] string? xslt = null,
        [FromBody] Identifiers? identifiers = null)
    {
        Response.Headers.Append("X-QueriedServices", "WL");

        var userInfo = await userClient.GetUserInfoAsync(new GetUserInfoRequest() { UserId = User.Id() });

        var requestedId = identifiers?.IdentifierList?.FirstOrDefault()?.ObjectId;
        var author = CreateAuthor(userInfo, requestedId);
        var feed = new LiveFeed()
        {
            Title = $"What's New with {author.Name}",
            Id = this.Url.Link("activities_route", new { }),
            Updated = DateTime.UtcNow,
            Author = author,
            Links =
            [
                new Link(this.Url.Link("activities_route", new { })),
            ]
        };

        var provider = await activityProvider.GetActivityProviderAsync();
        if (provider == null)
            return feed;

        // The device selects whose feed via the ObjectId: the owner's own CID -> the owner's feed;
        // any other CID is a contact card asking for that contact's feed (its linked accounts).
        var ownerCid = long.Parse(userInfo.Cid, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var requested = requestedId != null ? long.Parse(requestedId, CultureInfo.InvariantCulture) : (long?)null;

        if (requested is { } contactCid && contactCid != ownerCid)
        {
            feed.Entries.AddRange(
                await feeds.RenderContactFeedAsync(Url, provider, contactCid, count, User.Id()!));
        }
        else
        {
            feed.Entries.AddRange(
                await feeds.RenderFeedAsync(Url, provider, ActivitiesContext.My, count, author, User.Id()!));
        }

        return feed;
    }

    [HttpGet]
    [Produces("application/atom+xml")]
    [Route("/ContactsActivities", Name = "contacts_activities_route")]
    public async Task<ActionResult<LiveFeed>> ContactsActivities(
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Source")] string source = "WL",
        [FromQuery(Name = "Type")] string type = "all",
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "$xslt")] string? xslt = null)
    {
        Response.Headers.Append("X-QueriedServices", "WL");

        var userInfo = await userClient.GetUserInfoAsync(new GetUserInfoRequest() { UserId = User.Id() });
        var author = CreateAuthor(userInfo);
        var feed = new LiveFeed()
        {
            Title = $"What's New with {author.Name}",
            Id = this.Url.Link("contacts_activities_route", new { }),
            Updated = DateTime.UtcNow,
            Author = author,
            Links =
            [
                new Link(this.Url.Link("contacts_activities_route_for_user", new { provider = "WL", id = author.Id })),
            ]
        };

        var provider = await activityProvider.GetActivityProviderAsync();
        if (provider == null)
            return feed;

        var ctx = type == "media" ? ActivitiesContext.Media : ActivitiesContext.Contacts;
        feed.Entries.AddRange(
            await feeds.RenderFeedAsync(Url, provider, ctx, count, author, User.Id()!));

        return feed;
    }

    [HttpGet]
    [Produces("application/atom+xml")]
    [Route("/Activity({id})", Name = "activity")]
    public Task<ActionResult<LiveFeed>> Activity(
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Source")] string source = "WL",
        [FromQuery(Name = "Type")] string type = "all",
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "$xslt")] string? xslt = null)
    {
        return Task.FromResult<ActionResult<LiveFeed>>(NoContent());
    }

    [HttpGet]
    [Produces("application/atom+xml")]
    [Route("/Activity({id})/Replies", Name = "activity_replies")]
    public async Task<ActionResult<LiveCommentsFeed>> ActivityReplies(
        [FromRoute] string id,
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Source")] string source = "WL",
        [FromQuery(Name = "Type")] string type = "all",
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "$xslt")] string? xslt = null)
    {
        Response.Headers.Append("X-QueriedServices", "WL");

        var providerId = id[..id.IndexOf(':')];
        var stringId = id[(id.IndexOf(':') + 1)..];

        var activityInfo = new { id };
        var feed = new LiveCommentsFeed()
        {
            Title = "Replies",
            Id = this.Url.Link("activity_replies", activityInfo),
            Updated = DateTime.UtcNow,
            Links =
            [
                new Link(this.Url.Link("activity_replies", activityInfo)),
            ]
        };

        var activityProviderInstance = await activityProvider.GetActivityProviderAsync();
        if (activityProviderInstance == null)
            return feed;

        await foreach (var item in activityProviderInstance.GetRepliesAsync(providerId, stringId, Math.Min(count, 49)))
        {
            feed.Entries.Add(new LiveComment()
            {
                CommentId = $"{item.ProviderId}:{item.Id}",
                Title = null,
                Content = item.Content,
                Updated = item.Published.UtcDateTime,
                Author = new LiveCommentAuthor()
                {
                    Name = item.Author.DisplayName,
                    Cid = Cids.SynthesiseAuthorCid(item.Author.Provider, item.Author.Id).ToString(CultureInfo.InvariantCulture),
                }
            });
        }

        return feed;
    }

    [HttpPost]
    [Consumes("text/plain")]
    [Produces("application/atom+xml")]
    [Route("/Activity({id})/Replies", Name = "activity_replies")]
    public async Task<ActionResult> ActivityReplies(
       [FromRoute] string id)
    {
        using var reader = new StreamReader(Request.Body);
        var text = await reader.ReadToEndAsync();
        if (text == null)
            return BadRequest();

        Response.Headers.Append("X-QueriedServices", "WL");

        var providerId = id[..id.IndexOf(':')];
        var stringId = id[(id.IndexOf(':') + 1)..];

        var activityProviderInstance = await activityProvider.GetActivityProviderAsync();
        if (activityProviderInstance == null)
            return NoContent();

        await activityProviderInstance.CreateReplyAsync(providerId, stringId, text);

        return NoContent();
    }

    // TODO: move this to an adapter class
    private LiveAuthor CreateAuthor(GetUserInfoResponse userInfo, string? requestedCid = null)
    {
        var userId = long.Parse(userInfo.Cid, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var requestedId = requestedCid != null ? long.Parse(requestedCid, CultureInfo.InvariantCulture) : (long?)null;
        // A differing id is a contact-feed request (the ObjectId names the contact), not an error.
        if (requestedId != null && userId != requestedId)
            logger.LogDebug("Feed requested for contact CID {RequestedCid} (owner {OwnerCid})", requestedId, userId);

        return new LiveAuthor()
        {
            Id = $"{(requestedId ?? userId)}",
            Name = userInfo.Username,
            Url = this.Url.Link("activities_route_for_user", new { id = (requestedId ?? userId).ToString(), provider = "WL" }),
            Links = []
        };
    }
}
