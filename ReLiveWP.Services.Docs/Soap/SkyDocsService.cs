using ReLiveWP.Services.Docs.Dav;
using ReLiveWP.Services.Grpc.SkyDocs;

namespace ReLiveWP.Services.Docs.Soap;

public class SkyDocsService(SkyDocs.SkyDocsClient client,
                            IHttpContextAccessor httpContextAccessor,
                            ILogger<SkyDocsService> logger) : ISkyDocsService
{
    private const int SyncInterval = 300;

    public async Task<GetWebAccountInfoResponse> GetWebAccountInfo(GetWebAccountInfoRequest request)
    {
        var baseUri = BaseUri();
        var libraries = await client.ListLibrariesAsync(new ListDocLibrariesRequest());

        var mapped = libraries.Libraries
            .Where(l => !request.GetReadWriteLibrariesOnly || l.AccessLevel == DocAccessLevel.ReadWrite)
            .Select(l => new Library
            {
                AccessLevel = l.AccessLevel == DocAccessLevel.ReadWrite ? AccessLevel.ReadWrite : AccessLevel.Read,
                DavUrl = DavUrls.Build(baseUri, l.Path),
                DisplayName = l.DisplayName,
                ResourceId = l.ResourceId,
                WebUrl = $"{baseUri}/browse/{Uri.EscapeDataString(l.Path)}",
                LastModifiedDate = DateTimeOffset.FromUnixTimeSeconds(l.ModifiedUnix).UtcDateTime,
                SharingLevelInfo = new SharingLevelInfo { Level = SharingLevel.Private, Description = "Shared with: Just me" },
            })
            .ToArray();

        logger.LogInformation("GetWebAccountInfo returning {Count} libraries", mapped.Length);

        return new GetWebAccountInfoResponse
        {
            AccountTitle = libraries.AccountTitle,
            SignedInUser = libraries.SignedInUser,
            RootDavUrl = DavUrls.Build(baseUri, ""),
            NewLibraryUrl = $"{baseUri}/browse",
            Libraries = mapped,
            Documents = [],
            ProductInfo = BuildProductInfo(baseUri),
        };
    }

    public async Task<GetChangesSinceTokenResponse> GetChangesSinceToken(GetChangesSinceTokenRequest request)
    {
        var baseUri = BaseUri();
        var path = DavUrls.ToPath(request.DavUrl);

        var changes = await client.GetChangesAsync(new GetDocChangesRequest
        {
            Path = path,
            SyncToken = request.SyncToken ?? "",
        });

        var entries = changes.Changed
            .Select(item => ToEntry(baseUri, item))
            .Concat(changes.DeletedPaths.Select(deleted => ToDeletedEntry(baseUri, deleted)))
            .ToList();

        logger.LogInformation("GetChangesSinceToken for {Path}: {Changed} changed, {Deleted} deleted",
                              path, changes.Changed.Count, changes.DeletedPaths.Count);

        return new GetChangesSinceTokenResponse
        {
            MinAmIAloneSyncInterval = SyncInterval,
            MinBackgroundSyncInterval = SyncInterval,
            MinRealtimeSyncInterval = SyncInterval,
            SyncData = new SyncData { Entries = entries },
            SyncToken = changes.SyncToken,
        };
    }

    public Task<GetProductInfoResponse> GetProductInfo(GetProductInfoRequest request)
    {
        var info = BuildProductInfo(BaseUri());

        return Task.FromResult(new GetProductInfoResponse
        {
            HomePageUrl = info.HomePageUrl,
            IsSoapEnabled = info.IsSoapEnabled,
            IsSyncEnabled = info.IsSyncEnabled,
            LearnMoreUrl = info.LearnMoreUrl,
            ProductName = info.ProductName,
            ServiceDisabledErrorMessage = info.ServiceDisabledErrorMessage,
            ShortProductName = info.ShortProductName,
            SignInMessage = info.SignInMessage,
            SignUpMessage = info.SignUpMessage,
            SignUpUrl = info.SignUpUrl,
        });
    }

    public Task<ResolveWebUrlResponse> ResolveWebUrl(ResolveWebUrlRequest request)
    {
        var baseUri = BaseUri();
        var path = DavUrls.ToPath(request.WebUrl);

        return Task.FromResult(new ResolveWebUrlResponse { DavUrl = DavUrls.Build(baseUri, path) });
    }

    private static ProductInfo BuildProductInfo(string baseUri) => new()
    {
        HomePageUrl = baseUri,
        IsSoapEnabled = true,
        IsSyncEnabled = false,
        LearnMoreUrl = baseUri,
        ProductName = "ReLiveWP SkyDrive",
        ShortProductName = "ReLiveWP",
        ServiceDisabledErrorMessage = "This feature is currently not available. Please try again later.",
        SignInMessage = "Windows Live ID",
        SignUpMessage = "Don't have a Windows Live ID?",
        SignUpUrl = baseUri,
    };

    private static MultiStatusEntry ToEntry(string baseUri, DocItem item) => new(
        DavUrls.Build(baseUri, item.Path),
        item.Name,
        item.IsFolder,
        item.Size,
        DateTimeOffset.FromUnixTimeSeconds(item.CreatedUnix),
        DateTimeOffset.FromUnixTimeSeconds(item.ModifiedUnix),
        item.ReadOnly,
        item.ResourceId,
        string.IsNullOrEmpty(item.ProgId) ? null : item.ProgId);

    private static MultiStatusEntry ToDeletedEntry(string baseUri, string path) => new(
        DavUrls.Build(baseUri, path),
        DavUrls.Name(path),
        false,
        0,
        DateTimeOffset.UnixEpoch,
        DateTimeOffset.UnixEpoch,
        false,
        "",
        null,
        true);

    private string BaseUri()
    {
        var request = httpContextAccessor.HttpContext?.Request
            ?? throw new InvalidOperationException("No active request.");

        return $"{request.Scheme}://{request.Host}";
    }
}
