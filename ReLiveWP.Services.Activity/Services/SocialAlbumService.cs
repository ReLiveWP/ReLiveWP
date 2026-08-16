namespace ReLiveWP.Services.Activity.Services;

public record SocialAlbumFolder(string Title, IReadOnlyList<SocialPhoto> Photos);

public class SocialAlbumService(SocialAlbums providers,
                                ConnectionLookup connections,
                                ActivityProviderService activityProvider,
                                ILogger<SocialAlbumService> logger)
{
    public async Task<IReadOnlyList<SocialAlbum>> OwnedAlbumsAsync(string userId, CancellationToken ct = default)
    {
        var linked = await connections.AllAsync(ct);

        var albums = new List<SocialAlbum>();
        foreach (var provider in providers.All)
            albums.AddRange(await provider.GetAlbumsAsync(userId, linked, ct));

        return albums;
    }

    public async Task<IReadOnlyList<SocialAlbum>> SharedAlbumsAsync(long subjectCid, string userId,
                                                                    CancellationToken ct = default)
    {
        var sources = await activityProvider.GetContactPhotoSourcesAsync(subjectCid, userId, ct);

        var albums = new List<SocialAlbum>();
        foreach (var source in sources)
        {
            if (providers.For(source.Provider) is not { } provider)
                continue;

            // one dead account must not sink the listing, a 5xx here is retried for hours
            var handle = source.Handle;
            try
            {
                handle ??= await provider.GetHandleAsync(userId, source.ExternalId, connection: null, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "could not name the {Provider} album for {ExternalId}", source.Provider, source.ExternalId);
            }

            albums.Add(new SocialAlbum(
                SocialAlbumRef.Album(source.Provider, source.ExternalId),
                provider.TitleFor(handle)));
        }

        return albums;
    }

    public Task<bool> IsServableAsync(SocialAlbumProviderBase provider, string externalId, long? subjectCid,
                                      string userId, CancellationToken ct = default)
        => activityProvider.IsServableIdentityAsync(provider.Provider, externalId, subjectCid, userId, ct);

    public async Task<SocialAlbumFolder> FolderAsync(SocialAlbumProviderBase provider, string externalId, string userId,
                                                     CancellationToken ct = default)
    {
        var linked = await connections.AllAsync(ct);
        var owned = linked.FirstOrDefault(c => c.Service == provider.Provider && c.UserId == externalId);

        // an account that will not talk to us opens empty, a 5xx here is retried for hours
        SocialAlbumContents contents;
        try
        {
            contents = await provider.GetAlbumAsync(userId, externalId, owned, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "could not read the {Provider} album for {ExternalId}", provider.Provider, externalId);
            contents = new SocialAlbumContents([], null);
        }

        return new SocialAlbumFolder(provider.TitleFor(contents.Handle ?? owned?.UserName), contents.Photos);
    }
}
