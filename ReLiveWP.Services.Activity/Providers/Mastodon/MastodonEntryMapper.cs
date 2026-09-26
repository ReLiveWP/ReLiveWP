using Microsoft.AspNetCore.StaticFiles;
using ReLiveWP.ServiceDefaults.Outbound;
using ReLiveWP.Services.Activity.Models;

namespace ReLiveWP.Services.Activity.Providers.Mastodon;

public static class MastodonEntryMapper
{
    public const string ProviderId = "MA";
    public const string IdentityProviderToken = "mastodon";

    private const int MaxIdLength = 64;
    private const int MaxDisplayNameLength = 200;

    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    public static string ComposeActivityId(Uri instance, string statusId) => $"{instance.IdnHost}+{statusId}";

    public static bool TryParseActivityId(string provider, string activityId, out Uri instance, out string statusId)
    {
        instance = null!;
        statusId = "";

        if (!string.Equals(provider, ProviderId, StringComparison.OrdinalIgnoreCase))
            return false;

        var parts = activityId.Split('+');
        if (parts.Length != 2 || !IsInstanceId(parts[1]))
            return false;

        if (!ExternalRequestGuard.TryCreateInstanceRoot(parts[0], out var root) ||
            !string.Equals(root.IdnHost, parts[0], StringComparison.OrdinalIgnoreCase))
            return false;

        instance = root;
        statusId = parts[1];
        return true;
    }

    public static bool IsInstanceId(string? id)
        => id is { Length: > 0 and <= MaxIdLength } && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-');

    // what goes in a feed: boosts, replies and direct messages stay out, same as bluesky reposts and replies
    public static EntryModel? CreateFeedEntry(MastodonStatus status, Uri instance, Uri authorActorUri, string? selfActorUri)
    {
        if (status.Reblog != null || status.InReplyToId != null)
            return null;

        return MapStatus(status, instance, authorActorUri, selfActorUri);
    }

    public static EntryModel? MapStatus(MastodonStatus status, Uri instance, Uri authorActorUri, string? selfActorUri)
    {
        if (!IsInstanceId(status.Id) || status.Reblog != null || status.Visibility == "direct")
            return null;

        var author = MapAuthor(status.Account, instance, authorActorUri, selfActorUri);
        var photos = MapPhotos(status.MediaAttachments);

        var entry = new EntryModel()
        {
            Id = ComposeActivityId(instance, status.Id),
            ProviderId = ProviderId,
            EntryType = EntryType.Post,
            Title = "Post",
            Content = DescribeContent(status),
            Published = status.CreatedAt ?? DateTimeOffset.UtcNow,
            Author = author,
            Categories = ["status"],
            Generator = "Mastodon",
            CanonicalUrl = AcceptableOrEmpty(status.Url ?? status.Uri),
            CanReply = true,
            ReplyCount = status.RepliesCount,
        };

        if (photos.Count > 0)
        {
            entry.Categories.Add("media");
            entry.Categories.Add("photo");
            entry.AdditionalActivities.AddRange(photos);
        }

        return entry;
    }

    public static string DescribeAccountAddress(MastodonAccount account, Uri instance)
        => FediverseHandle.DescribeAccountAddress(account.Username, account.Fqn, account.Acct, instance);

    public static string DescribeDisplayName(MastodonAccount account, string accountAddress)
    {
        var displayName = string.IsNullOrWhiteSpace(account.DisplayName) ? $"@{accountAddress}" : account.DisplayName.Trim();
        return displayName.Length > MaxDisplayNameLength ? displayName[..MaxDisplayNameLength] : displayName;
    }

    public static string AcceptableOrEmpty(string? url)
        => ExternalRequestGuard.TryParseAcceptableUri(url, out var uri) ? uri.AbsoluteUri : "";

    private static ProfileModel MapAuthor(MastodonAccount account, Uri instance, Uri authorActorUri, string? selfActorUri)
    {
        var address = DescribeAccountAddress(account, instance);

        return new ProfileModel()
        {
            IsMe = selfActorUri != null && string.Equals(authorActorUri.AbsoluteUri, selfActorUri, StringComparison.Ordinal),
            Provider = IdentityProviderToken,
            Id = authorActorUri.AbsoluteUri,
            ScreenName = $"@{address}",
            DisplayName = DescribeDisplayName(account, address),
            CanonicalUrl = AcceptableOrEmpty(account.Url),
            AvatarUrl = AcceptableOrEmpty(account.Avatar),
        };
    }

    private static string DescribeContent(MastodonStatus status)
    {
        var body = StatusHtmlConverter.ToPlainText(status.Content);
        if (string.IsNullOrWhiteSpace(status.SpoilerText))
            return body;

        var warning = status.SpoilerText.Trim();
        return body.Length == 0 ? $"CW: {warning}" : $"CW: {warning}\n\n{body}";
    }

    private static List<PhotoActivityModel> MapPhotos(MastodonMediaAttachment[]? attachments)
    {
        List<PhotoActivityModel> photos = [];
        foreach (var attachment in attachments ?? [])
        {
            if (attachment.Type != "image")
                continue;

            if (!ExternalRequestGuard.TryParseAcceptableUri(attachment.Url, out var full))
                continue;

            var preview = ExternalRequestGuard.TryParseAcceptableUri(attachment.PreviewUrl, out var thumbnail) ? thumbnail : full;

            photos.Add(new PhotoActivityModel()
            {
                Id = IsInstanceId(attachment.Id) ? attachment.Id! : full.AbsoluteUri,
                ThumbnailUrl = preview.AbsoluteUri,
                FullSizeUrl = full.AbsoluteUri,
                CanonicalUrl = full.AbsoluteUri,
                MimeType = GuessImageMimeType(full),
            });
        }

        return photos;
    }

    private static string GuessImageMimeType(Uri url)
    {
        if (ContentTypes.TryGetContentType(url.AbsolutePath, out var contentType) &&
            contentType.StartsWith("image/", StringComparison.Ordinal))
            return contentType;

        return "image/jpeg";
    }
}
