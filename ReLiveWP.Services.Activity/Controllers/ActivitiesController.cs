using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading;
using Atom.Xml;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.Com.Atproto.Repo;
using FishyFlip.Models;
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
    [Route("/Users({id})/Status")]
    [Produces("application/atom+xml")]
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
    [Route("/Activities", Name = "activities_route")]
    [Route("/Users({provider}:{id})/Activities", Name = "activities_route_for_user")]
    [Produces("application/atom+xml")]
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

        var provider = await GetProtocolAsync();
        if (provider == null)
            return feed;

        await foreach (var item in provider.GetEntriesAsync(ActivitiesContext.My, count))
        {
            var liveEntry = CreatePostEntry(provider, item, author);
            if (liveEntry == null)
                continue;

            feed.Entries.Add(liveEntry);
        }

        return feed;
    }

    [HttpGet]
    [Authorize]
    [Route("/ContactsActivities", Name = "contacts_activities_route")]
    [Route("/Users({provider}:{id})/ContactsActivities", Name = "contacts_activities_route_for_user")]
    [Produces("application/atom+xml")]
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
                new Link(this.Url.Link("contacts_activities_route", new { })),
            ]
        };

        var provider = await GetProtocolAsync();
        if (provider == null)
            return feed;

        await foreach (var item in provider.GetEntriesAsync(type == "media" ? ActivitiesContext.Media : ActivitiesContext.Contacts, count))
        {
            var liveEntry = CreatePostEntry(provider, item, author);
            if (liveEntry == null)
                continue;

            feed.Entries.Add(liveEntry);
        }

        return feed;
    }

    [HttpGet]
    [Authorize]
    [Route("/Activity({provider}:{id})", Name = "activity")]
    [Produces("application/atom+xml")]
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
    [Route("/Activity({provider}:{id})/Replies", Name = "activity_replies")]
    [Produces("application/atom+xml")]
    public async Task<ActionResult<LiveFeed>> ActivityReplies(
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Source")] string source = "WL",
        [FromQuery(Name = "Type")] string type = "all",
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "$xslt")] string? xslt = null)
    {
        return NoContent();
    }

    private async Task<BlueskyActivityProvider?> GetProtocolAsync()
    {
        var auth = Request.Headers.Authorization.ToString();
        var authHeader = string.Concat("Bearer ", auth.AsSpan(auth.IndexOf(' ')));

        var headers = new Metadata() { { "Authorization", authHeader } };
        var servicesResponse = await connectedServices.GetConnectionsAsync(new ConnectionsRequest(), headers);
        var service = servicesResponse.Connections.FirstOrDefault(s => s.Service == "atproto");
        if (service is null)
        {
            return null;
        }

        return ActivatorUtilities.CreateInstance<BlueskyActivityProvider>(serviceProvider, authHeader, service);
    }

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

    private LiveEntry? CreatePostEntry(ActivityProviderBase provider, EntryModel entryModel, LiveAuthor meAuthor)
    {
        var entryAuthor = entryModel.Author;
        var author = new LiveAuthor()
        {
            Id = $"{provider.ProviderId}:{entryAuthor.Id}", // HORRIBLE
            Name = entryAuthor.DisplayName,
            Url = entryAuthor.CanonicalUrl,
            Links =
            [
                new Link(entryAuthor.AvatarUrl, "preview", "image/jpeg")
            ]
        };

        var activityInfo = new { provider = provider.ProviderId, id = entryModel.Id };
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

            ActivityVerb = "http://activitystrea.ms/schema/1.0/post",
            Activities =
            {
                new()
                {
                    ObjectType = "http://activitystrea.ms/schema/1.0/status",
                    Id = id,
                    Title = entryModel.Title,
                    Content = entryModel.Content,
                }
            },
            ActivityId = entryModel.Id,

            AppId = "6262816084389410",
            ChangeType = "0",
            SourceId = "WL",
            ServiceActivityId = entryModel.Id,
            Reactions = []
        };

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
