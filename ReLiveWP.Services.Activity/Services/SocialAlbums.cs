namespace ReLiveWP.Services.Activity.Services;

public class SocialAlbums(IEnumerable<SocialAlbumProviderBase> providers)
{
    private readonly Dictionary<string, SocialAlbumProviderBase> byToken =
        providers.ToDictionary(p => p.Provider, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<SocialAlbumProviderBase> All => byToken.Values;

    public SocialAlbumProviderBase? For(string provider)
        => byToken.TryGetValue(provider, out var found) ? found : null;

    public bool TryResolveAlbum(string resourceId, out SocialAlbumProviderBase provider, out string externalId)
    {
        provider = null!;
        return SocialAlbumRef.TryParseAlbum(resourceId, out var token, out externalId)
               && For(token) is { } found
               && found.IsValidExternalId(externalId)
               && (provider = found) != null;
    }

    public bool TryResolvePhoto(string resourceRef, out SocialAlbumProviderBase provider, out string externalId, out string mediaId)
    {
        provider = null!;
        return SocialAlbumRef.TryParsePhoto(resourceRef, out var token, out externalId, out mediaId)
               && For(token) is { } found
               && found.IsValidExternalId(externalId)
               && found.IsValidMediaId(mediaId)
               && (provider = found) != null;
    }
}
