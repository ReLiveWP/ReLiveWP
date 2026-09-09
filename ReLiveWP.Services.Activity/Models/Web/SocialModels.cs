using ReLiveWP.Services.Activity.Providers;

namespace ReLiveWP.Services.Activity.Models.Web;

public record SocialAuthor(
    string? Cid,
    bool IsMe,
    string Provider,
    string ExternalId,
    string DisplayName,
    string ScreenName,
    string AvatarUrl,
    string CanonicalUrl)
{
    public static SocialAuthor From(ProfileModel author, string? cid) => new(
        author.IsMe ? null : cid,
        author.IsMe,
        author.Provider,
        author.Id,
        author.DisplayName,
        author.ScreenName,
        author.AvatarUrl,
        author.CanonicalUrl);
}

public record SocialPhoto(string ThumbnailUrl, string FullSizeUrl, string CanonicalUrl, string MimeType)
{
    public static SocialPhoto From(PhotoActivityModel photo) =>
        new(photo.ThumbnailUrl, photo.FullSizeUrl, photo.CanonicalUrl, photo.MimeType);
}

public record SocialEntry(
    string Id,
    string ProviderId,
    string Type,
    DateTimeOffset Published,
    string Title,
    string Content,
    string Generator,
    string CanonicalUrl,
    IReadOnlyList<string> Categories,
    bool CanReply,
    int? ReplyCount,
    SocialAuthor Author,
    IReadOnlyList<SocialPhoto> Photos)
{
    public static SocialEntry From(EntryModel entry, string? authorCid) => new(
        $"{entry.ProviderId}:{entry.Id}",
        entry.ProviderId,
        entry.EntryType.ToString().ToLowerInvariant(),
        entry.Published,
        entry.Title,
        entry.Content,
        entry.Generator,
        entry.CanonicalUrl,
        entry.Categories,
        entry.CanReply,
        entry.ReplyCount,
        SocialAuthor.From(entry.Author, authorCid),
        [.. entry.AdditionalActivities.OfType<PhotoActivityModel>().Select(SocialPhoto.From)]);
}

public record SocialFeedResponse(bool Connected, IReadOnlyList<SocialEntry> Entries);

public record SocialContactFeedResponse(string Cid, bool Shared, IReadOnlyList<SocialEntry> Entries);

public record SocialRepliesResponse(string ActivityId, IReadOnlyList<SocialEntry> Entries);

public record SocialReplyRequest(string ActivityId, string Text);

public record SocialReplyResponse(bool Posted);

public record SocialIdentity(string Provider, string ExternalId, string Handle, string DisplayName, string AvatarUrl)
{
    public static SocialIdentity From(ResolvedIdentity identity) =>
        new(identity.Provider, identity.ExternalId, identity.Handle, identity.DisplayName, identity.AvatarUrl);
}

public record SocialIdentitiesResponse(string Cid, IReadOnlyList<SocialIdentity> Identities);

public record SocialBindRequest(string Provider, string Handle);

public record SocialErrorResponse(string Error);

public record SocialAlbumSummary(string ResourceId, string Title, string? CoverUrl);

public record SocialAlbumPhoto(
    string Ref,
    string Kind,
    string ThumbnailUrl,
    string FullSizeUrl,
    string? Alt,
    DateTimeOffset Created,
    int Width,
    int Height);

public record SocialAlbumsResponse(IReadOnlyList<SocialAlbumSummary> Albums);

public record SocialContactAlbumsResponse(string Cid, bool Shared, IReadOnlyList<SocialAlbumSummary> Albums);

public record SocialAlbumResponse(string ResourceId, string Title, IReadOnlyList<SocialAlbumPhoto> Photos);
