using ReLiveWP.Services.Activity.Models;

namespace ReLiveWP.Services.Activity.Providers.Mastodon;

public static class MastodonNotificationMapper
{
    public static NotificationKind KindForType(string? type) => type switch
    {
        "favourite" => NotificationKind.Like,
        "reblog" => NotificationKind.Repost,
        "follow" or "follow_request" => NotificationKind.Follow,
        "mention" => NotificationKind.Mention,
        "poll" => NotificationKind.Poll,
        "update" => NotificationKind.Edit,
        _ => NotificationKind.Unknown,
    };

    public static EntryModel? Create(MastodonNotification notification, Uri instance, Uri authorActorUri)
    {
        if (!MastodonEntryMapper.IsInstanceId(notification.Id) || notification.Account is not { } account)
            return null;

        var kind = KindForType(notification.Type);
        if (!NotificationEntries.IsMention(kind))
            return null;

        var address = MastodonEntryMapper.DescribeAccountAddress(account, instance);
        var status = notification.Status;

        var source = new NotificationSource(
            Id: MastodonEntryMapper.ComposeActivityId(instance, notification.Id),
            Kind: kind,
            Published: notification.CreatedAt ?? DateTimeOffset.UtcNow,
            ActorId: authorActorUri.AbsoluteUri,
            ActorDisplayName: MastodonEntryMapper.DescribeDisplayName(account, address),
            ActorScreenName: $"@{address}",
            ActorAvatarUrl: MastodonEntryMapper.AcceptableOrEmpty(account.Avatar),
            ActorCanonicalUrl: MastodonEntryMapper.AcceptableOrEmpty(account.Url),
            SubjectText: status == null ? null : StatusHtmlConverter.ToPlainText(status.Content),
            SubjectUrl: status == null
                ? MastodonEntryMapper.AcceptableOrEmpty(account.Url)
                : MastodonEntryMapper.AcceptableOrEmpty(status.Url ?? status.Uri));

        return NotificationEntries.Create(
            MastodonEntryMapper.ProviderId, MastodonEntryMapper.IdentityProviderToken, "Mastodon", source);
    }
}
