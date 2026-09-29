using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Activity.Models.Atom;
using ReLiveWP.Services.Activity.Providers;
using ReLiveWP.Services.Activity.Utilities;
using Link = Atom.Xml.Link;

namespace ReLiveWP.Services.Activity.Services;

public class FeedRendererService(ActivityFeedReader reader, MediaProxyUrlSigner mediaProxy)
{
    private const string NotificationNetworkName = "Twitter";

    public async Task<List<LiveEntry>> RenderFeedAsync(
        IUrlHelper url,
        OwnedActivityProviderBase provider,
        ActivitiesContext context,
        int count,
        LiveAuthor meAuthor,
        string userId,
        string storeSourceId = ActivitySubjects.LiveSourceId)
    {
        var entries = await reader.ReadOwnFeedAsync(provider, context, count, userId);
        return [.. entries.Select(resolved => CreatePostEntry(url, resolved.Entry, meAuthor, resolved.AuthorCid ?? 0, storeSourceId))];
    }

    // the notifications pivot: the author is always someone else, so there is no "me" shortcut
    public async Task<List<LiveEntry>> RenderNotificationsAsync(
        IUrlHelper url,
        OwnedActivityProviderBase provider,
        int count,
        DateTimeOffset? since,
        string userId,
        string storeSourceId,
        string storeObjectId)
    {
        var entries = await reader.ReadNotificationsAsync(provider, count, since, userId);
        return [.. entries.Select(resolved => CreateNotificationEntry(url, resolved.Entry, resolved.AuthorCid ?? 0, storeSourceId, storeObjectId))];
    }

    public async Task<List<LiveEntry>> RenderPinnedContactsFeedAsync(
        IUrlHelper url,
        IReadOnlyList<PublicActivityProviderBase> providers,
        IReadOnlyList<PinnedContactSources> contacts,
        int count)
    {
        var entries = await reader.ReadPinnedContactsFeedAsync(providers, contacts, count);
        return [.. entries.Select(resolved => CreatePostEntry(url, resolved.Entry, meAuthor: null, resolved.AuthorCid ?? 0))];
    }

    internal LiveEntry CreateNotificationEntry(IUrlHelper url, EntryModel entryModel, long authorCid,
                                               string storeSourceId, string storeObjectId)
    {
        var entry = CreatePostEntry(url, entryModel, meAuthor: null, authorCid, storeSourceId);
        entry.Generator = NotificationNetworkName;

        var statusObject = entry.Activities.First();
        statusObject.Entities =
        [
            new LiveEntity()
            {
                Type = LiveEntity.UserMentionType,
                Start = 0,
                End = 0,
                User = new LiveEntityUser() { ObjectId = storeObjectId },
            }
        ];

        return entry;
    }

    public async Task<List<LiveEntry>> RenderContactFeedAsync(
        IUrlHelper url,
        IReadOnlyList<PublicActivityProviderBase> providers,
        IReadOnlyList<ContactFeedSource> sources,
        long cid,
        int count,
        string storeSourceId = ActivitySubjects.LiveSourceId)
    {
        var entries = await reader.ReadContactFeedAsync(providers, sources, cid, count);
        return [.. entries.Select(resolved => CreatePostEntry(url, resolved.Entry, meAuthor: null, authorCid: cid, storeSourceId))];
    }

    internal LiveEntry CreatePostEntry(IUrlHelper url, EntryModel entryModel, LiveAuthor? meAuthor, long authorCid,
                                       string storeSourceId = ActivitySubjects.LiveSourceId)
    {
        var entryAuthor = entryModel.Author;
        var author = entryAuthor.IsMe && meAuthor != null ? meAuthor : new LiveAuthor()
        {
            Id = authorCid.ToString(CultureInfo.InvariantCulture),
            Name = entryAuthor.DisplayName,
            ScreenName = entryAuthor.ScreenName,
            Url = entryAuthor.CanonicalUrl,
            Links =
            [
                CreateImageLink(entryAuthor.AvatarUrl, "preview", MediaSize.Avatar, "image/jpeg")
            ]
        };

        var activityInfo = new { id = $"{entryModel.ProviderId}:{entryModel.Id}" };
        var id = url.Link("activity", activityInfo)!;

        var postEntry = new LiveEntry()
        {
            Id = id,
            Title = entryModel.Title,
            Summary = entryModel.Content,
            Published = entryModel.Published.UtcDateTime,
            Updated = entryModel.Published.UtcDateTime,
            Author = author,
            Links =
            [
                new Link(url.Link("activity_replies", activityInfo), "replies", "application/atom+xml")
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
            SourceId = storeSourceId,
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
                        CreateImageLink(photo.ThumbnailUrl, "preview", MediaSize.Thumb, photo.MimeType),
                        CreateImageLink(photo.FullSizeUrl, "alternate", MediaSize.Full, photo.MimeType)
                    ]
                });
            }
        }

        return postEntry;
    }

    private Link CreateImageLink(string sourceUrl, string rel, MediaSize size, string sourceMimeType)
    {
        var proxiedUrl = mediaProxy.SignUrlOrOriginal(sourceUrl, size);
        if (proxiedUrl == sourceUrl)
            return new Link(sourceUrl, rel, sourceMimeType);

        return new Link(proxiedUrl, rel, MediaProfiles.OutputContentType);
    }
}
