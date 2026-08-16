namespace ReLiveWP.Services.Activity.Services;

// "{provider}+{externalId}" for an album, plus "+{mediaId}" for one photo. splitting is the same
// everywhere, what counts as a valid id is the provider's business
public static class SocialAlbumRef
{
    public static string Album(string provider, string externalId) => $"{provider}+{externalId}";

    public static string Photo(string provider, string externalId, string mediaId)
        => $"{provider}+{externalId}+{mediaId}";

    public static bool TryParseAlbum(string resourceId, out string provider, out string externalId)
    {
        provider = externalId = "";

        var split = resourceId.IndexOf('+');
        if (split <= 0 || split == resourceId.Length - 1)
            return false;

        provider = resourceId[..split];
        externalId = resourceId[(split + 1)..];
        return true;
    }

    public static bool TryParsePhoto(string resourceRef, out string provider, out string externalId, out string mediaId)
    {
        externalId = mediaId = "";

        if (!TryParseAlbum(resourceRef, out provider, out var rest))
            return false;

        var split = rest.LastIndexOf('+');
        if (split <= 0 || split == rest.Length - 1)
            return false;

        externalId = rest[..split];
        mediaId = rest[(split + 1)..];
        return true;
    }
}
