using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Services;
using ReLiveWP.Dav;

namespace ReLiveWP.Backend.ConnectedServices.OAuthProviders;

public class CalDavCredentialProvider(DavHomeSetDiscovery discovery,
                                      IOutboundAddressPolicy addressPolicy,
                                      ConnectionSecretProtector protector,
                                      ILogger<CalDavCredentialProvider> logger) : ICredentialLinkProvider
{
    public async Task<LiveConnectedService> LinkAsync(LiveConnectedService connection, CredentialLink credentials,
                                                      CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(credentials.Username) || string.IsNullOrWhiteSpace(credentials.Secret))
            throw new CredentialLinkException("A username and password are required.");

        var baseUri = WebDavCredentialProvider.NormaliseUri(credentials.ServiceUrl);
        addressPolicy.ValidateUri(baseUri);

        var homeSet = await discovery.DiscoverAsync(
            baseUri, credentials, DavProps.CalendarHomeSet, "CalDAV", ct);

        connection.Service = CalDav.SERVICE_NAME;
        connection.ServiceUrl = homeSet.ToString();
        connection.AccessToken = "";
        connection.RefreshToken = "";
        connection.ExpiresAt = DateTimeOffset.MaxValue;
        connection.Flags = LiveConnectedServiceFlags.None;
        connection.EncryptedSecret = protector.Protect(credentials.Secret);
        connection.ServiceProfile = new LiveConnectedServiceProfile
        {
            UserId = $"{credentials.Username}@{baseUri.Host}",
            Username = credentials.Username,
            DisplayName = baseUri.Host,
            Label = string.IsNullOrWhiteSpace(credentials.Label)
                ? WebDavCredentialProvider.DescribeShare(homeSet, credentials.Username)
                : credentials.Label.Trim(),
        };

        logger.LogInformation("Linked CalDAV calendar home at {HomeSet} for {Username}", homeSet, credentials.Username);

        return connection;
    }
}
