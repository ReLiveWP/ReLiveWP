using System.Xml.Linq;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Services;
using ReLiveWP.Dav;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Backend.ConnectedServices.OAuthProviders;

public class CardDavCredentialProvider(IHttpClientFactory httpClientFactory,
                                       IOutboundAddressPolicy addressPolicy,
                                       ConnectionSecretProtector protector,
                                       ILogger<CardDavCredentialProvider> logger) : ICredentialLinkProvider
{
    private static readonly string PrincipalBody = DavBody.Propfind(DavProps.CurrentUserPrincipal);

    private static readonly string HomeSetBody = DavBody.Propfind(DavProps.AddressbookHomeSet);

    public async Task<LiveConnectedService> LinkAsync(LiveConnectedService connection, CredentialLink credentials,
                                                      CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(credentials.Username) || string.IsNullOrWhiteSpace(credentials.Secret))
            throw new CredentialLinkException("A username and password are required.");

        var baseUri = WebDavCredentialProvider.NormaliseUri(credentials.ServiceUrl);
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
        var principal = await PropfindHrefAsync(baseUri, PrincipalBody, DavProps.CurrentUserPrincipal, credentials, ct);
        if (principal is null)
            throw new CredentialLinkException("The server did not report a current-user-principal; is this a CardDAV server?");

        var principalUri = new Uri(baseUri, principal);
        addressPolicy.ValidateUri(principalUri);

        var homeSet = await PropfindHrefAsync(principalUri, HomeSetBody, DavProps.AddressbookHomeSet, credentials, ct);
        if (homeSet is null)
            throw new CredentialLinkException("The server did not report an addressbook-home-set for this account.");

        var homeSetUri = new Uri(principalUri, homeSet);
        addressPolicy.ValidateUri(homeSetUri);

        return homeSetUri;
    }

    private async Task<string?> PropfindHrefAsync(Uri uri, string body, XName property,
                                                  CredentialLink credentials, CancellationToken ct)
    {
        using var dav = DavCredentials.CreateClient(httpClientFactory, credentials.Username, credentials.Secret);

        DavMultiStatus multistatus;
        try
        {
            multistatus = await dav.PropfindAsync(uri.ToString(), body, depth: "0", ct);
        }
        catch (HttpRequestException e)
        {
            throw new CredentialLinkException($"Could not reach {uri.Host}: {e.Message}");
        }
        catch (DavParseException e)
        {
            throw new CredentialLinkException($"The server returned XML we could not read: {e.Message}");
        }
        catch (DavException e) when (e.Status is 401 or 403)
        {
            throw new CredentialLinkException("The server rejected that username and password.");
        }
        catch (DavException e)
        {
            throw new CredentialLinkException($"The server answered {e.Status} to a PROPFIND.");
        }

        return multistatus.Responses.Select(r => r.HrefValue(property)).FirstOrDefault(v => v is not null);
    }
}
