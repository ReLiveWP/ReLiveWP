using System.Net;
using Atom.Xml;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Models;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Controllers;

public class ActivitiesController(
    ILogger<ActivitiesController> logger,
    ILogger<ATProtocol> atprotoLogger,
    User.UserClient userClient,
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
        var headers = new Metadata() { { "Authorization", "Bearer " + auth.Substring(auth.IndexOf(' ')) } };
        var userInfo = await userClient.GetUserInfoAsync(new GetUserInfoRequest() { UserId = User.Id() });

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
             .WithInstanceUrl(new Uri("http://127.0.0.4:5001"))
             .WithLogger(atprotoLogger)
             .EnableAutoRenewSession(false)
             .WithServiceEndpointUponLogin(false)
             .Build();

        protocol.Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + auth.Substring(auth.IndexOf(' ')));
        protocol.Client.DefaultRequestHeaders.Add("X-Connection-Id", service.Id);

        var did = ATDid.Create(service.UserId)!;
        var atProfile = (await protocol.Actor.GetProfileAsync(did))
            .HandleResult();

        if (atProfile == null)
            return NotFound();

        var author = CreateAuthor(atProfile);
        var feed = new LiveFeed()
        {
            Title = $"What's New with {author.Name}",
            Id = "https://api.live.int.relivewp.net/Users('WL:-6296795494865413357')/Activities",
            Updated = DateTime.UtcNow,
            Author = author,
            Links =
            [
                new Link("https://api.live.int.relivewp.net/Users('WL:-6296795494865413357')/Activities")
            ]
        };

        var cursor = "";

        do
        {
            var atFeed = (await protocol.Feed.GetAuthorFeedAsync(did, limit: Math.Max(10, count - feed.Entries.Count), cursor: cursor, includePins: false))
                .HandleResult()!;

            cursor = WebUtility.UrlEncode(atFeed.Cursor);

            foreach (var feedViewPost in atFeed.Feed)
            {
                if (feed.Entries.Count == count)
                    break;

                var entry = CreatePostEntry(feedViewPost, author);
                if (entry == null)
                    continue;

                feed.Entries.Add(entry);
            }
        } while (feed.Entries.Count < count && !string.IsNullOrWhiteSpace(cursor));

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            feed.Links.Add(new Link($"https://api.live.int.relivewp.net/Users('WL:-6296795494865413357')/Activities?cursor={cursor}", "next"));
        }

        return feed;
    }

    [HttpGet]
    [Authorize]
    [Route("/ContactsActivities")]
    [Produces("application/atom+xml")]
    public async Task<ActionResult<LiveFeed>> ContactsActivities(
        [FromQuery(Name = "Count")] int count = 10,
        [FromQuery(Name = "Source")] string source = "WL",
        [FromQuery(Name = "$format")] string format = "atom10",
        [FromQuery(Name = "$xslt")] string? xslt = null)
    {
        Response.Headers.Append("X-QueriedServices", "WL");

        var auth = Request.Headers.Authorization.ToString();
        var headers = new Metadata() { { "Authorization", "Bearer " + auth.Substring(auth.IndexOf(' ')) } };

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

        var atProto = new ATProtocolBuilder()
             .WithInstanceUrl(new Uri("http://127.0.0.4:5001"))
             .WithLogger(atprotoLogger)
             .EnableAutoRenewSession(false)
             .WithServiceEndpointUponLogin(false)
             .Build();

        atProto.Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + auth.Substring(auth.IndexOf(' ')));
        atProto.Client.DefaultRequestHeaders.Add("X-Connection-Id", service.Id);

        var did = ATDid.Create(service.UserId)!;
        var atProfile = (await atProto.Actor.GetProfileAsync(did))
            .HandleResult();

        if (atProfile == null)
            return NotFound();

        var author = CreateAuthor(atProfile);
        var feed = new LiveFeed()
        {
            Title = $"What's New with {author.Name}",
            Id = "https://api.live.int.relivewp.net/Users('WL:-6296795494865413357')/ContactsActivities",
            Updated = DateTime.UtcNow,
            Author = author,
            Links =
            [
                new Link("https://api.live.int.relivewp.net/Users('WL:-6296795494865413357')/ContactsActivities"),
            ]
        };


        var cursor = "";
        do
        {
            var atFeed = (await atProto.GetTimelineAsync(limit: Math.Max(10, count - feed.Entries.Count), cursor: cursor))
                .HandleResult()!;

            cursor = WebUtility.UrlEncode(atFeed.Cursor);

            foreach (var feedViewPost in atFeed.Feed)
            {
                if (feed.Entries.Count == count)
                    break;

                var entry = CreatePostEntry(feedViewPost);
                if (entry == null)
                    continue;

                feed.Entries.Add(entry);
            }
        } while (feed.Entries.Count < count && !string.IsNullOrWhiteSpace(cursor));

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            feed.Links.Add(new Link($"https://api.live.int.relivewp.net/Users('WL:-6296795494865413357')/ContactsActivities?cursor={cursor}", "next"));
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

    private static LiveEntry? CreatePostEntry(FeedViewPost feedViewPost, LiveAuthor? author = null)
    {
        if (feedViewPost.Post is not { Record: Post post } postView || feedViewPost.Reply is { })
            return null;

        author ??= new LiveAuthor()
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
            Id = $"https://api.live.int.relivewp.net/Users('WL:{author.Id}')/Activities('WL:{WebUtility.UrlEncode(postId)}')",
            Title = "Post",
            Summary = post.Text,
            Published = post.CreatedAt,
            Updated = post.CreatedAt,
            Author = author,
            Links =
            [
                new Link(author.Url + $"/post/{WebUtility.UrlEncode(postId)}", "alternate", "text/html"),
            ],
            Categories = [new("status")],
            Generator = "Bluesky",
            ActivityVerb = "http://activitystrea.ms/schema/1.0/post",
            Activities =
            {
                new()
                {
                    ObjectType = "http://activitystrea.ms/schema/1.0/status",
                    Id = $"https://api.live.int.relivewp.net/Users('WL:{author.Id}')/Activities('WL:{WebUtility.UrlEncode(postId)}')",
                    Title = "Post",
                    Content = post.Text,
                }
            },
            ActivityId = postId,
            AppId = "6262816084389410",
            ChangeType = "3",
            SourceId = "WL",
            ServiceActivityId = postId,
            Reactions = postView.ReplyCount?.ToString() ?? ""
        };

        if (postView.Embed is ViewImages viewImages)
        {
            postEntry.Categories.Add(new Category("media"));
            postEntry.Categories.Add(new Category("photo"));

            foreach (var image in viewImages.Images)
            {
                postEntry.Activities.Add(new ActivityObject()
                {
                    ObjectType = "http://activitystrea.ms/schema/1.0/photo",
                    Id = image.Fullsize,
                    Links =
                    [
                        new Link(image.Thumb, "preview", image.Type),
                        new Link(image.Fullsize, "alternate", image.Type),
                    ]
                });
            }
        }

        return postEntry;
    }
}
