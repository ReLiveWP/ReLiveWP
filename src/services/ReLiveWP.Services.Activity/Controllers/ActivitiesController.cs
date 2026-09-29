using System.Globalization;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Activity.Models.Atom;
using ReLiveWP.Services.Activity.Providers;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Activity.Utilities;
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
    FeedRendererService feeds,
    ActivityProviderService activityProvider) : Controller
{
    private const string SocialNotificationsXslt = "wp7socnots";
    private const string PinnedContactXslt = "wp7ctsm";

    [HttpPost]
    [Route("/Activities", Name = "activities_route")]
    public async Task<ActionResult<LiveFeed>> Activities(
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Type")] string type = "all",
        [FromQuery(Name = "$xslt")] string? xslt = null,
        [FromQuery(Name = "$xslt_time")] string? xsltTime = null,
        [FromBody] Identifiers? identifiers = null)
    {
        Response.Headers.Append("X-QueriedServices", "WL");

        var userInfo = await userClient.GetUserInfoAsync(new GetUserInfoRequest() { UserId = User.Id() });

        var ownerCid = long.Parse(userInfo.Cid, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var identifier = identifiers?.IdentifierList?.FirstOrDefault();
        var subject = ActivitySubjects.Resolve(identifier?.SourceId, identifier?.ObjectId, ownerCid);

        var author = CreateAuthor(userInfo, subject);
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

        if (string.Equals(xslt, SocialNotificationsXslt, StringComparison.OrdinalIgnoreCase))
        {
            var storeObjectId = identifier?.ObjectId ?? ownerCid.ToString(CultureInfo.InvariantCulture);
            return await NotificationsAsync(feed, author, subject, storeObjectId, count, xsltTime);
        }

        switch (subject.Kind)
        {
            case ActivitySubjectKind.Contact:
                var sources = await activityProvider.GetContactFeedSourcesAsync(subject.Cid, User.Id()!, HttpContext.RequestAborted);
                feed.Entries.AddRange(
                    await feeds.RenderContactFeedAsync(Url, activityProvider.PublicProviders, sources, subject.Cid, count, subject.StoreSourceId));
                return feed;

            case ActivitySubjectKind.Unknown:
                logger.LogInformation("Feed requested for {SourceId}:{ObjectId}, which names nobody we know",
                    identifier?.SourceId, identifier?.ObjectId);
                return feed;
        }

        var provider = await activityProvider.GetOwnedProviderAsync(OwnedProviderUse.Read);
        if (provider == null)
            return feed;

        feed.Entries.AddRange(
            await feeds.RenderFeedAsync(Url, provider, ActivitiesContext.My, count, author, User.Id()!, subject.StoreSourceId));

        return feed;
    }

    private async Task<ActionResult<LiveFeed>> NotificationsAsync(
        LiveFeed feed, LiveAuthor author, ActivitySubject subject, string storeObjectId, int count, string? xsltTime)
    {
        feed.Title = $"Notifications for {author.Name}";

        var provider = await activityProvider.GetOwnedProviderAsync(OwnedProviderUse.Read);
        if (provider == null)
            return feed;

        var since = ParseLastSeen(xsltTime);
        logger.LogInformation("Social notifications requested for {Store}, count {Count}, since {Since}",
            subject.StoreSourceId, count, since);

        feed.Entries.AddRange(
            await feeds.RenderNotificationsAsync(Url, provider, count, since, User.Id()!, subject.StoreSourceId, storeObjectId));

        return feed;
    }

    private async Task<ActionResult<LiveFeed>> PinnedContactsAsync(LiveFeed feed, int count, string? xsltPeeps)
    {
        var peeps = PinnedPeeps.ParsePeeps(xsltPeeps);
        var cids = PinnedPeeps.SelectLiveCids(peeps);
        if (cids.Count < peeps.Count)
            logger.LogInformation("Pinned contact feed for {Peeps} skipped {Skipped} peep(s) that are not distinct Live cids",
                xsltPeeps, peeps.Count - cids.Count);

        var lookups = cids.Select(async cid =>
        {
            var sources = await activityProvider.GetContactFeedSourcesAsync(cid, User.Id()!, HttpContext.RequestAborted);
            return new PinnedContactSources(cid, sources);
        });
        var contacts = await Task.WhenAll(lookups);

        feed.Entries.AddRange(
            await feeds.RenderPinnedContactsFeedAsync(Url, activityProvider.PublicProviders, contacts, count));

        return feed;
    }

    private DateTimeOffset? ParseLastSeen(string? xsltTime)
    {
        if (string.IsNullOrWhiteSpace(xsltTime))
            return null;

        if (!DateTimeOffset.TryParse(xsltTime, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
        {
            logger.LogInformation("Could not read the notifications marker {Marker}", xsltTime);
            return null;
        }

        return parsed <= DateTimeOffset.UnixEpoch ? null : parsed;
    }

    [HttpGet]
    [Produces("application/atom+xml")]
    [Route("/ContactsActivities", Name = "contacts_activities_route")]
    public async Task<ActionResult<LiveFeed>> ContactsActivities(
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Source")] string source = "WL",
        [FromQuery(Name = "Type")] string type = "all",
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "$xslt")] string? xslt = null,
        [FromQuery(Name = "$xslt_peeps")] string? xsltPeeps = null)
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

        if (string.Equals(xslt, PinnedContactXslt, StringComparison.OrdinalIgnoreCase))
            return await PinnedContactsAsync(feed, count, xsltPeeps);

        var provider = await activityProvider.GetOwnedProviderAsync(OwnedProviderUse.Read);
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

        if (!ActivityIds.TrySplit(id, out var providerId, out var stringId))
            return BadRequest();

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

        var providers = await activityProvider.GetReplyProvidersAsync();
        var replies = providers
            .ToAsyncEnumerable()
            .SelectMany(p => p.GetRepliesAsync(providerId, stringId, Math.Min(count, 49)));

        await foreach (var item in replies)
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

        if (!ActivityIds.TrySplit(id, out var providerId, out var stringId))
            return BadRequest();

        var activityProviderInstance = await activityProvider.GetOwnedProviderAsync(OwnedProviderUse.Post);
        if (activityProviderInstance == null)
            return NoContent();

        await activityProviderInstance.CreateReplyAsync(providerId, stringId, text);

        return NoContent();
    }

    // TODO: move this to an adapter class
    private LiveAuthor CreateAuthor(GetUserInfoResponse userInfo, ActivitySubject? subject = null)
    {
        var ownerCid = long.Parse(userInfo.Cid, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var cid = subject is { Kind: ActivitySubjectKind.Contact } contact ? contact.Cid : ownerCid;
        if (cid != ownerCid)
            logger.LogDebug("Feed requested for contact CID {RequestedCid} (owner {OwnerCid})", cid, ownerCid);

        return new LiveAuthor()
        {
            Id = $"{cid}",
            Name = userInfo.Username,
            Url = this.Url.Link("activities_route_for_user", new { id = cid.ToString(), provider = "WL" }),
            Links = []
        };
    }
}
