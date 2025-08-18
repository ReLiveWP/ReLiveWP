using System.Net;
using System.Reflection;
using System.Text;
using Atom.Xml;
using Duende.IdentityModel.Client;
using FishyFlip;
using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.Com.Atproto.Repo;
using FishyFlip.Lexicon.Place.Stream;
using FishyFlip.Models;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Razor.Templating.Core;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Controllers;

public class ActivitiesController(
    ILogger<ActivitiesController> logger,
    ILogger<ATProtocol> atprotoLogger,
    ConnectedServices.ConnectedServicesClient connectedServices) : Controller
{
    [HttpPost]
    [Authorize]
    [Route("/Activities")]
    [Produces("application/atom+xml")]
    public async Task<ActionResult<LiveFeed>> Activities(
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "$xslt")] string? xslt = null)
    {
        Response.Headers.Append("X-QueriedServices", "WL");

        var auth = Request.Headers.Authorization.ToString();
        var headers = new Metadata() { { "Authorization", "Bearer" + auth.Substring(auth.IndexOf(' ')) } };

        var connectedServicesRequest = new ConnectionsRequest() { };
        var servicesResponse = await connectedServices.GetConnectionsAsync(connectedServicesRequest, headers);
        if (servicesResponse.Connections.Count == 0)
        {
            return NoContent();
        }

        var service = servicesResponse.Connections.FirstOrDefault(s => s.Service == "atproto");
        if (service is null)
        {
            return NoContent();
        }

        var protocol = new ATProtocolBuilder()
             .WithInstanceUrl(new Uri(service.ServiceUrl))
             .WithLogger(atprotoLogger)
             .EnableAutoRenewSession(false)
             .WithServiceEndpointUponLogin(false)
             .Build();

        var atProfile = (await protocol.Actor.GetProfileAsync(session.Did))
            .HandleResult();

        if (atProfile == null)
            return NotFound();

        var author = CreateAuthor(atProfile);

        var atFeed = (await protocol.Feed.GetAuthorFeedAsync(session.Did, limit: count, includePins: false))
            .HandleResult();

        var feed = new LiveFeed()
        {
            Title = $"What's New with {author.Name}",
            Id = "https://api.live.int.relivewp.net/Users('WL:-6296795494865413357')/Activities",
            Updated = DateTime.UtcNow,
            Author = author,
            Links =
            [
                new Link("https://api.live.int.relivewp.net/Users('WL:-6296795494865413357')/Activities"),
                new Link($"https://api.live.int.relivewp.net/Users('WL:-6296795494865413357')/Activities?cursor={WebUtility.UrlEncode(atFeed.Cursor)}", "next")
            ]
        };

        foreach (var feedViewPost in atFeed.Feed)
        {
            var entry = CreatePostEntry(feedViewPost);

            if (entry == null) continue;
            feed.Entries.Add(entry);
        }

        return feed;
    }

    [HttpGet]
    [Route("/ContactsActivities")]
    [Produces("application/atom+xml")]
    public async Task<ActionResult<LiveFeed>> ContactsActivities(
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Source")] string source = "WL",
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "$xslt")] string? xslt = null)
    {
        Response.Headers.Append("X-QueriedServices", "WL");
        var actor = new ATDid("did:plc:7rfssi44thh6f4ywcl3u5nvt");

        var atProtocolBuilder = new ATProtocolBuilder()
            .EnableAutoRenewSession(true)
            .WithLogger(atprotoLogger);

        var atProto = atProtocolBuilder.Build();
        var atProfile = (await atProto.Actor.GetProfileAsync(actor))
            .HandleResult();

        if (atProfile == null)
            return NotFound();

        LiveAuthor author = CreateAuthor(atProfile);

        var atFeed = (await atProto.Feed.GetFeedAsync(new ATUri("at://did:plc:z72i7hdynmk6r22z27h6tvur/app.bsky.feed.generator/whats-hot"), limit: count))
            .HandleResult();

        var feed = new LiveFeed()
        {
            Title = $"What's New with {author.Name}",
            Id = "https://api.live.int.relivewp.net/Users('WL:-6296795494865413357')/ContactsActivities",
            Updated = DateTime.UtcNow,
            Author = author,
            Links =
            [
                new Link("https://api.live.int.relivewp.net/Users('WL:-6296795494865413357')/ContactsActivities"),
                new Link($"https://api.live.int.relivewp.net/Users('WL:-6296795494865413357')/ContactsActivities?cursor={WebUtility.UrlEncode(atFeed.Cursor)}", "next")
            ]
        };

        foreach (var feedViewPost in atFeed.Feed)
        {
            var entry = CreatePostEntry(feedViewPost);

            if (entry == null) continue;
            feed.Entries.Add(entry);
        }

        return feed;
    }

    private static LiveAuthor CreateAuthor(ProfileViewDetailed atProfile)
    {
        return new LiveAuthor()
        {
            Id = "-6296795494865413357",
            Name = atProfile.DisplayName,
            Url = $"https://bsky.app/profile/{atProfile.Did}",
            Links =
            [
                new Link(atProfile.Avatar, "preview", "image/jpeg")
            ]
        };
    }

    private static LiveEntry? CreatePostEntry(FeedViewPost feedViewPost)
    {
        if (feedViewPost.Post is not { Record: Post post } postView || feedViewPost.Reply is { })
            return null;

        var author = new LiveAuthor()
        {
            Id = "-" + postView.Author.Did.ToString().GetHashCode(), // HORRIBLE
            Name = postView.Author.DisplayName,
            Url = $"https://bsky.app/profile/{postView.Author.Did}",
            Links =
            [
                new Link(postView.Author.Avatar, "preview", "image/jpeg")
            ]
        };

        var postId = postView.Uri.Rkey;
        var postEntry = new LiveEntry()
        {
            Id = $"https://api.live.int.relivewp.net/Users('WL:-6296795494865413357')/Activities('WL:{WebUtility.UrlEncode(postId)}')",
            Title = "Post",
            Summary = post.Text,
            Published = post.CreatedAt,
            Updated = post.CreatedAt,
            Author = author,
            Links =
            [
                new Link(author.Url + "/post/{WebUtility.UrlEncode(postId)}", "alternate", "text/html"),
            ],
            Category = new Category("status"),
            Generator = "Bluesky",
            ActivityVerb = "http://activitystrea.ms/schema/1.0/post",
            ActivityObject = new ActivityObject()
            {
                ObjectType = "http://activitystrea.ms/schema/1.0/status",
                Id = $"https://api.live.int.relivewp.net/Users('WL:-6296795494865413357')/Activities('WL:{WebUtility.UrlEncode(postId)}')",
                Title = "Post",
                Content = post.Text,
            },
            ActivityId = postId,
            AppId = "6262816084389410",
            ChangeType = "3",
            SourceId = "WL",
            ServiceActivityId = postId,
        };

        return postEntry;
    }
}
