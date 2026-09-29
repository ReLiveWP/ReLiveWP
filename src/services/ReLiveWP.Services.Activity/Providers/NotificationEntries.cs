using ReLiveWP.Services.Activity.Models;

namespace ReLiveWP.Services.Activity.Providers;

// what a provider has to dig out of its own notification shape; everything after this is common
public sealed record NotificationSource(
    string Id,
    NotificationKind Kind,
    DateTimeOffset Published,
    string ActorId,
    string ActorDisplayName,
    string ActorScreenName,
    string ActorAvatarUrl,
    string ActorCanonicalUrl,
    string? SubjectText,
    string SubjectUrl);

public static class NotificationEntries
{
    public static bool IsMention(NotificationKind kind)
        => kind is NotificationKind.Mention or NotificationKind.Reply or NotificationKind.Quote;

    public static EntryModel Create(string providerId, string identityProvider, string generator, NotificationSource source)
    {
        var sentence = NotificationPhrases.Describe(source.ActorDisplayName, source.Kind);

        return new EntryModel()
        {
            Id = source.Id,
            ProviderId = providerId,
            EntryType = EntryType.Post,
            Title = sentence,
            Content = string.IsNullOrWhiteSpace(source.SubjectText) ? sentence : source.SubjectText.Trim(),
            Published = source.Published,
            Author = new ProfileModel()
            {
                IsMe = false,
                Provider = identityProvider,
                Id = source.ActorId,
                DisplayName = source.ActorDisplayName,
                ScreenName = source.ActorScreenName,
                AvatarUrl = source.ActorAvatarUrl,
                CanonicalUrl = source.ActorCanonicalUrl,
            },
            Categories = ["status"],
            Generator = generator,
            CanonicalUrl = source.SubjectUrl,
            CanReply = false,
            ReplyCount = 0,
        };
    }
}
