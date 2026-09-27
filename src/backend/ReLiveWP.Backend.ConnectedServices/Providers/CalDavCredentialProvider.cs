using ReLiveWP.Backend.ConnectedServices.Services;
using ReLiveWP.Dav;
using ReLiveWP.ServiceDefaults.Outbound;

namespace ReLiveWP.Backend.ConnectedServices.Providers;

public class CalDavCredentialProvider(DavHomeSetDiscovery discovery,
                                      IOutboundAddressPolicy addressPolicy,
                                      ConnectionSecretProtector protector,
                                      ILogger<CalDavCredentialProvider> logger)
    : DavCredentialProviderBase(CalDav.SERVICE_NAME, "CalDAV", addressPolicy, protector, logger)
{
    protected override Task<Uri> ResolveCollectionAsync(Uri baseUri, CredentialLink credentials, CancellationToken ct)
        => discovery.DiscoverAsync(baseUri, credentials, DavProps.CalendarHomeSet, DisplayName, ct);
}
