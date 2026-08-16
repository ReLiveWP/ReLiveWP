using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Services;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Backend.ConnectedServices.OAuthProviders;

public class CardDavCredentialProvider(IHttpClientFactory httpClientFactory,
                                       IOutboundAddressPolicy addressPolicy,
                                       ConnectionSecretProtector protector,
                                       ILogger<CardDavCredentialProvider> logger) : ICredentialLinkProvider
{
    private static readonly HttpMethod Propfind = new("PROPFIND");
    private static readonly XNamespace Dav = "DAV:";
    private static readonly XNamespace Card = "urn:ietf:params:xml:ns:carddav";

    private const string PrincipalBody =
        """<?xml version="1.0" encoding="utf-8"?><D:propfind xmlns:D="DAV:"><D:prop><D:current-user-principal/></D:prop></D:propfind>""";

    private const string HomeSetBody =
        """<?xml version="1.0" encoding="utf-8"?><D:propfind xmlns:D="DAV:" xmlns:C="urn:ietf:params:xml:ns:carddav"><D:prop><C:addressbook-home-set/></D:prop></D:propfind>""";

    public async Task<LiveConnectedService> LinkAsync(LiveConnectedService connection, CredentialLink credentials,
                                                      CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(credentials.Username) || string.IsNullOrWhiteSpace(credentials.Secret))
            throw new CredentialLinkException("A username and password are required.");

        var baseUri = NormaliseUri(credentials.ServiceUrl);
        addressPolicy.ValidateUri(baseUri);

        var homeSet = await DiscoverHomeSetAsync(baseUri, credentials, ct);
        connection.Service = CardDav.SERVICE_NAME;
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

        logger.LogInformation("Linked CardDAV address book at {HomeSet} for {Username}", homeSet, credentials.Username);

        return connection;
    }

    private async Task<Uri> DiscoverHomeSetAsync(Uri baseUri, CredentialLink credentials, CancellationToken ct)
    {
        var principal = await PropfindHrefAsync(baseUri, PrincipalBody, Dav + "current-user-principal", credentials, ct);
        if (principal is null)
            throw new CredentialLinkException("The server did not report a current-user-principal; is this a CardDAV server?");

        var principalUri = new Uri(baseUri, principal);
        addressPolicy.ValidateUri(principalUri);

        var homeSet = await PropfindHrefAsync(principalUri, HomeSetBody, Card + "addressbook-home-set", credentials, ct);
        if (homeSet is null)
            throw new CredentialLinkException("The server did not report an addressbook-home-set for this account.");

        var homeSetUri = new Uri(principalUri, homeSet);
        addressPolicy.ValidateUri(homeSetUri);

        return homeSetUri;
    }

    private async Task<string?> PropfindHrefAsync(Uri uri, string body, XName property,
                                                  CredentialLink credentials, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient(OutboundAddressPolicyExtensions.GuardedClientName);
        using var request = new HttpRequestMessage(Propfind, uri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/xml"),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{credentials.Username}:{credentials.Secret}")));
        request.Headers.TryAddWithoutValidation("Depth", "0");

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, ct);
        }
        catch (HttpRequestException e)
        {
            throw new CredentialLinkException($"Could not reach {uri.Host}: {e.Message}");
        }

        using (response)
        {
            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
                throw new CredentialLinkException("The server rejected that username and password.");

            var xml = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode && (int)response.StatusCode != 207)
                throw new CredentialLinkException($"The server answered {(int)response.StatusCode} to a PROPFIND.");

            try
            {
                return XDocument.Parse(xml).Descendants(property)
                    .Descendants(Dav + "href")
                    .Select(x => (string?)x)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            }
            catch (Exception e)
            {
                throw new CredentialLinkException($"The server returned XML we could not read: {e.Message}");
            }
        }
    }

    private static Uri NormaliseUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new CredentialLinkException("A server address is required.");

        var trimmed = value.Trim();
        if (!trimmed.Contains("://", StringComparison.Ordinal))
            trimmed = "https://" + trimmed;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            throw new CredentialLinkException("That server address is not a valid URL.");

        return uri;
    }
}
