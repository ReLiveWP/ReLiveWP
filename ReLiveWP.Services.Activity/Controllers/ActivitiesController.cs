using FishyFlip.Lexicon.Com.Whtwnd.Blog;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Activity.Models.Atom;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Grpc;
using Link = Atom.Xml.Link;

namespace ReLiveWP.Services.Activity.Controllers;

public class ActivitiesController(
    IServiceProvider serviceProvider,
    User.UserClient userClient,
    ConnectedServices.ConnectedServicesClient connectedServices) : Controller
{
    [HttpPost]
    [Authorize]
    [Produces("application/atom+xml")]
    [Route("/Users({id})/Status")]
    public async Task<ActionResult> PostAsync(long id, [FromBody] LiveEntry entry)
    {
        Response.Headers.Append("X-QueriedServices", "WL");
        //var (protocol, did) = await GetProtocolAsync();

        //var record = new Post
        //{
        //    Text = entry.Title.Value,
        //    CreatedAt = DateTime.UtcNow
        //};

        //_ = (await protocol.CreateRecordAsync(did, "app.bsky.feed.post", record, cancellationToken: HttpContext.RequestAborted))
        //    .HandleResult();

        return NoContent();
    }

    [HttpPost]
    [Authorize]
    [Produces("application/atom+xml")]
    [Route("/Activities", Name = "activities_route")]
    [Route("/Users({provider}:{id})/Activities", Name = "activities_route_for_user")]
    public async Task<ActionResult<LiveFeed>> Activities(
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Type")] string type = "all",
        [FromQuery(Name = "$xslt")] string? xslt = null)
    {
        Response.Headers.Append("X-QueriedServices", "WL");

        var userInfo = await userClient.GetUserInfoAsync(new GetUserInfoRequest() { UserId = User.Id() });
        var author = CreateAuthor(userInfo);
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

        var provider = await GetAllFeedsProviderAsync();
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
    [Authorize]
    [Produces("application/atom+xml")]
    [Route("/ContactsActivities", Name = "contacts_activities_route")]
    [Route("/Users({provider}:{id})/ContactsActivities", Name = "contacts_activities_route_for_user")]
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
                new Link(this.Url.Link("contacts_activities_route_for_user", new { provider = "WL", id = userInfo.Puid.ToString() })),
            ]
        };

        var provider = await GetAllFeedsProviderAsync();
        if (provider == null)
        {
            // TODO: add a system thing to say "link accounts"
            return feed;
        }

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
    [Authorize]
    [Produces("application/atom+xml")]
    [Route("/Activity({provider}:{id})", Name = "activity")]
    public async Task<ActionResult<LiveFeed>> Activity(
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Source")] string source = "WL",
        [FromQuery(Name = "Type")] string type = "all",
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "$xslt")] string? xslt = null)
    {
        return NoContent();
    }

    [HttpGet]
    [Authorize]
    [Produces("application/atom+xml")]
    [Route("/Activity({provider}:{id})/Replies", Name = "activity_replies")]
    public async Task<ActionResult<LiveFeed>> ActivityReplies(
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Source")] string source = "WL",
        [FromQuery(Name = "Type")] string type = "all",
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "$xslt")] string? xslt = null)
    {
        return NoContent();
    }

    private async Task<ActivityProviderBase?> GetAllFeedsProviderAsync()
    {
        var auth = Request.Headers.Authorization.ToString();
        var authHeader = string.Concat("Bearer ", auth.AsSpan(auth.IndexOf(' ')));

        var headers = new Metadata() { { "Authorization", authHeader } };
        var servicesResponse = await connectedServices.GetConnectionsAsync(new ConnectionsRequest(), headers);

        List<BlueskyActivityProvider> providers = [];
        foreach (var connection in servicesResponse.Connections)
        {
            if (connection.Service == "atproto")
            {
                providers.Add(ActivatorUtilities.CreateInstance<BlueskyActivityProvider>(serviceProvider, authHeader, connection));
            }
        }

        return new FeedCoalescingActivityProvider([.. providers]);
    }

    // TODO: move this to an adapter class
    private LiveAuthor CreateAuthor(GetUserInfoResponse userInfo)
    {
        return new LiveAuthor()
        {
            Id = $"{(long)userInfo.Puid}",
            Name = userInfo.Username,
            Url = this.Url.Link("activities_route_for_user", new { id = userInfo.Puid, provider = "WL" }),
            Links = []
        };
    }

    private LiveEntry? CreatePostEntry(EntryModel entryModel, LiveAuthor meAuthor)
    {
        var entryAuthor = entryModel.Author;
        var author = new LiveAuthor()
        {
            //Id = $"{provider.ProviderId}:{entryAuthor.Id}", // HORRIBLE
            Name = entryAuthor.DisplayName,
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
