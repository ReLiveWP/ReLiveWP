using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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

        await foreach (var item in provider.GetEntriesAsync(ActivitiesContext.My, count))
        {
            var liveEntry = CreatePostEntry(item, author);
            if (liveEntry == null)
                continue;

            feed.Entries.Add(liveEntry);
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

        await foreach (var item in provider.GetEntriesAsync(type == "media" ? ActivitiesContext.Media : ActivitiesContext.Contacts, count))
        {
            var liveEntry = CreatePostEntry(item, author);
            if (liveEntry == null)
                continue;

            feed.Entries.Add(liveEntry);
        }

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
                    Cid = SynthesiseCid(item.Author.Id).ToString(CultureInfo.InvariantCulture),
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

    private static long SynthesiseCid(string identity)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(identity));
        return BitConverter.ToInt64(hash, 0) & long.MaxValue;
    }

    // TODO: move this to an adapter class
    private LiveAuthor CreateAuthor(GetUserInfoResponse userInfo, string? requestedCid = null)
    {
        var userId = long.Parse(userInfo.Cid, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var requestedId = requestedCid != null ? long.Parse(requestedCid, CultureInfo.InvariantCulture) : (long?)null;
        if (requestedId != null && userId != requestedId)
            logger.LogError("Requested PUID and User PUID do not match!! {RequestedPuid} != {UserPuid}", requestedCid, userInfo.Cid);

        return new LiveAuthor()
        {
            Id = $"{(requestedId ?? userId)}",
            Name = userInfo.Username,
            Url = this.Url.Link("activities_route_for_user", new { id = (requestedId ?? userId).ToString(), provider = "WL" }),
            Links = []
        };
    }

    private LiveEntry? CreatePostEntry(EntryModel entryModel, LiveAuthor meAuthor)
    {
        var entryAuthor = entryModel.Author;
        var author = entryAuthor.IsMe ? meAuthor : new LiveAuthor()
        {
            Id = entryAuthor.IsMe ? meAuthor.Id : null, // TODO: this will eventually do some funky "Windows Live" mapping
            Name = entryAuthor.DisplayName,
            ScreenName = entryAuthor.ScreenName,
            Url = entryAuthor.CanonicalUrl,
            Links =
            [
                new Link(entryAuthor.AvatarUrl, "preview", "image/jpeg")
            ]
        };

        var activityInfo = new { id = $"{entryModel.ProviderId}:{entryModel.Id}" };
        var id = this.Url.Link("activity", activityInfo)!;

        var postEntry = new LiveEntry()
        {
            Id = id,
            Title = entryModel.Title,
            Summary = entryModel.Content,
            Published = entryModel.Published.UtcDateTime,
            Updated = entryModel.Published.UtcDateTime,
            Author = entryAuthor.IsMe ? meAuthor : author,
            Links =
            [
                new Link(this.Url.Link("activity_replies", activityInfo), "replies", "application/atom+xml")
                {
                    Count = entryModel.ReplyCount?.ToString() ?? ""
                },
                new Link(entryModel.CanonicalUrl, "alternate", "text/html"),
            ],
            Categories = [.. entryModel.Categories.Select(c => new LiveCategory(c))],
            Generator = entryModel.Generator,

            ActivityVerb = entryModel.EntryType switch
            {
                EntryType.Article => "http://activitystrea.ms/schema/1.0/article",
                _ => "http://activitystrea.ms/schema/1.0/post",
            },
            Activities = [],

            ActivityId = entryModel.Id,
            AppId = "6262816084389410",
            ChangeType = "0",
            SourceId = "WL",
            ServiceActivityId = entryModel.Id,
            Reactions = []
        };

        if (entryModel.EntryType == EntryType.Post)
        {
            postEntry.Activities.Add(new()
            {
                ObjectType = "http://activitystrea.ms/schema/1.0/status",
                Id = id,
                Title = entryModel.Title,
                Content = entryModel.Content,
            });
        }

        foreach (var item in entryModel.AdditionalActivities)
        {
            if (item is PhotoActivityModel photo)
            {
                postEntry.Activities.Add(new LiveActivityObject()
                {
                    ObjectType = "http://activitystrea.ms/schema/1.0/photo",
                    Id = photo.CanonicalUrl,
                    Links =
                    [
                        new Link(photo.ThumbnailUrl, "preview", photo.MimeType),
                        new Link(photo.FullSizeUrl, "alternate", photo.MimeType)
                    ]
                });
            }
        }

        return postEntry;
    }
}
