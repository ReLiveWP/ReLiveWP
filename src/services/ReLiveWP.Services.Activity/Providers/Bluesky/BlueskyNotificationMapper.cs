using ReLiveWP.Services.Activity.Models;

namespace ReLiveWP.Services.Activity.Providers.Bluesky;

public static class BlueskyNotificationMapper
{
    public static NotificationKind KindForReason(string? reason) => reason switch
    {
        "like" => NotificationKind.Like,
        "repost" => NotificationKind.Repost,
        "follow" => NotificationKind.Follow,
        "mention" => NotificationKind.Mention,
        "reply" => NotificationKind.Reply,
        "quote" => NotificationKind.Quote,
        _ => NotificationKind.Unknown,
    };

    // the notification's own record uri, in the same {identity}+{collection}+{rkey} shape as a post id
    public static string ComposeActivityId(string identity, string collection, string rkey)
        => $"{identity}+{collection}+{rkey}";

    public static EntryModel Create(NotificationSource source)
        => NotificationEntries.Create(BlueskyEntryMapper.ProviderId, BlueskyEntryMapper.IdentityProviderToken, "Bluesky", source);

    public static NotificationSource CreateSource(
        string id,
        string? reason,
        DateTimeOffset published,
        string did,
        string handle,
        string? displayName,
        string? avatar,
        string? subjectText,
        string subjectUrl)
    {
        var screenName = $"@{handle}";

        return new NotificationSource(
            Id: id,
            Kind: KindForReason(reason),
            Published: published,
            ActorId: did,
            ActorDisplayName: string.IsNullOrWhiteSpace(displayName) ? screenName : displayName,
            ActorScreenName: screenName,
            ActorAvatarUrl: BlueskyEntryMapper.FixImageUrl(avatar) ?? "",
            ActorCanonicalUrl: BlueskyEntryMapper.DescribeProfileUrl(did),
            SubjectText: subjectText,
            SubjectUrl: subjectUrl);
    }
}
