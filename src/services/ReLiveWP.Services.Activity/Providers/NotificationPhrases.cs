namespace ReLiveWP.Services.Activity.Providers;

public enum NotificationKind
{
    Unknown,
    Like,
    Repost,
    Follow,
    Mention,
    Reply,
    Quote,
    Poll,
    Edit,
}

public static class NotificationPhrases
{
    public static string Describe(string actor, NotificationKind kind) => kind switch
    {
        NotificationKind.Mention => $"{actor} mentioned you",
        NotificationKind.Reply => $"{actor} replied to your post",
        NotificationKind.Quote => $"{actor} quoted your post",
        _ => $"{actor} interacted with you",
    };
}
