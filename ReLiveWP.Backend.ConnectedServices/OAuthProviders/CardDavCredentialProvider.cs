using ReLiveWP.Backend.ConnectedServices.Services;
using ReLiveWP.Dav;

namespace ReLiveWP.Backend.ConnectedServices.OAuthProviders;

public class CardDavCredentialProvider(DavHomeSetDiscovery discovery,
                                       IOutboundAddressPolicy addressPolicy,
                                       ConnectionSecretProtector protector,
                                       ILogger<CardDavCredentialProvider> logger)
    : DavCredentialProviderBase(CardDav.SERVICE_NAME, "CardDAV", addressPolicy, protector, logger)
{
    protected override Task<Uri> ResolveCollectionAsync(Uri baseUri, CredentialLink credentials, CancellationToken ct)
        => discovery.DiscoverAsync(baseUri, credentials, DavProps.AddressbookHomeSet, DisplayName, ct);
}
