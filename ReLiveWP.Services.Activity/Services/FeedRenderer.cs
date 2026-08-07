using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Activity.Models;
using ReLiveWP.Services.Activity.Models.Atom;
using ReLiveWP.Services.Grpc.Mailbox;
using Link = Atom.Xml.Link;

namespace ReLiveWP.Services.Activity.Services;

public class FeedRenderer(
    MailboxStore.MailboxStoreClient mailbox, 
    ILogger<FeedRenderer> logger)
{
    public async Task<List<LiveEntry>> RenderFeedAsync(
        IUrlHelper url,
        ActivityProviderBase provider,
        ActivitiesContext context,
        int count,
        LiveAuthor meAuthor,
        string userId)
    {
        var entries = await provider.GetEntriesAsync(context, count).ToListAsync();
        var cidByAuthor = await ResolveAuthorCidsAsync(entries, userId);

        return [.. entries.Select(entry =>
        {
            var cid = entry.Author.IsMe ? 0 : cidByAuthor[(entry.Author.Provider, entry.Author.Id)];
            return CreatePostEntry(url, entry, meAuthor, cid);
        })];
    }

    public async Task<List<LiveEntry>> RenderContactFeedAsync(
        IUrlHelper url,
        ActivityProviderBase provider,
        long cid,
        int count,
        string userId)
    {
        var identities = await mailbox.ResolveContactIdentitiesAsync(new ResolveContactIdentitiesRequest
        {
            UserId = userId,
            Cids = { cid },
        });

        if (identities.Identities.Count == 0)
        {
            logger.LogInformation("Per-contact feed for CID {Cid}: no linked identities", cid);
            return [];
        }

        var entries = await identities.Identities
            .ToAsyncEnumerable()
            .SelectMany(identity => provider.GetAuthorEntriesAsync(identity.Provider, identity.ExternalId, count))
            .ToListAsync();

        logger.LogInformation(
            "Per-contact feed for CID {Cid}: {Identities} identities, {Entries} entries",
            cid, identities.Identities.Count, entries.Count);

        return [.. entries
            .OrderByDescending(e => e.Published)
            .Take(count)
            .Select(entry => CreatePostEntry(url, entry, meAuthor: null, authorCid: cid))];
    }

    private async Task<Dictionary<(string Provider, string Id), long>> ResolveAuthorCidsAsync(
        IReadOnlyList<EntryModel> entries, string userId)
    {
        var cidByAuthor = new Dictionary<(string Provider, string Id), long>();

        foreach (var group in entries.Where(e => !e.Author.IsMe).GroupBy(e => e.Author.Provider))
        {
            var provider = group.Key;
            var externalIds = group.Select(e => e.Author.Id).Distinct().ToList();

            var resolved = await mailbox.ResolveAuthorsToContactsAsync(new ResolveAuthorsToContactsRequest
            {
                UserId = userId,
                Provider = provider,
                ExternalIds = { externalIds },
            });

            logger.LogInformation(
                "Resolved {Count} {Provider} authors, {Bound} bound to contacts",
                externalIds.Count, provider, resolved.ContactCids.Count);

            foreach (var externalId in externalIds)
            {
                cidByAuthor[(provider, externalId)] = resolved.ContactCids.TryGetValue(externalId, out var contactCid)
                    ? contactCid
                    : Cids.SynthesiseAuthorCid(provider, externalId);
            }
        }

        return cidByAuthor;
    }
    
    private static LiveEntry CreatePostEntry(IUrlHelper url, EntryModel entryModel, LiveAuthor? meAuthor, long authorCid)
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
                new Link(entryAuthor.AvatarUrl, "preview", "image/jpeg")
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
