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
    [Route("/Activity({provider}:{id})", Name = "activity")]
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
    [Route("/Activity({provider}:{id})/Replies", Name = "activity_replies")]
    public Task<ActionResult<LiveFeed>> ActivityReplies(
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Source")] string source = "WL",
        [FromQuery(Name = "Type")] string type = "all",
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "$xslt")] string? xslt = null)
    {
        return Task.FromResult<ActionResult<LiveFeed>>(NoContent());
    }

    // TODO: move this to an adapter class
    private LiveAuthor CreateAuthor(GetUserInfoResponse userInfo, string? requestedPuid = null)
    {
        if (requestedPuid != null && userInfo.Puid.ToString() != requestedPuid)
            logger.LogError("Requested PUID and User PUID do not match!! {RequestedPuid} != {UserPuid}", requestedPuid, userInfo.Puid);

        return new LiveAuthor()
        {
            Id = $"{(requestedPuid ?? userInfo.Puid.ToString())}",
            Name = userInfo.Username,
            Url = this.Url.Link("activities_route_for_user", new { id = userInfo.Puid, provider = "WL" }),
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

        var activityInfo = new { provider = entryModel.ProviderId, id = entryModel.Id };
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
