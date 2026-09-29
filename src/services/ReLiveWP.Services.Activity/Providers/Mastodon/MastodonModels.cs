namespace ReLiveWP.Services.Activity.Providers.Mastodon;

public sealed record MastodonStatusRequest(string Status, string? InReplyToId = null, string? Visibility = null);

public sealed record MastodonAccount(
    string Id,
    string Username,
    string? Acct,
    string? Fqn,
    string? DisplayName,
    string? Avatar,
    string? Url,
    string? Uri);

public sealed record MastodonMediaDimensions(int? Width, int? Height);

public sealed record MastodonMediaMeta(MastodonMediaDimensions? Original);

public sealed record MastodonMediaAttachment(
    string? Id,
    string? Type,
    string? Url,
    string? PreviewUrl,
    string? Description,
    MastodonMediaMeta? Meta);

public sealed record MastodonStatus(
    string Id,
    DateTimeOffset? CreatedAt,
    string? Uri,
    string? Url,
    string? Content,
    string? SpoilerText,
    string? Visibility,
    string? InReplyToId,
    MastodonStatus? Reblog,
    int? RepliesCount,
    MastodonAccount Account,
    MastodonMediaAttachment[]? MediaAttachments,
    bool? Sensitive);

public sealed record MastodonNotification(
    string Id,
    string? Type,
    DateTimeOffset? CreatedAt,
    MastodonAccount? Account,
    MastodonStatus? Status);

public sealed record MastodonContext(MastodonStatus[]? Ancestors, MastodonStatus[]? Descendants);

public sealed record MastodonSearchResults(MastodonStatus[]? Statuses);

public sealed record MastodonAccountRef(Uri Instance, Uri ActorUri, string AccountAddress, MastodonAccount Account);
