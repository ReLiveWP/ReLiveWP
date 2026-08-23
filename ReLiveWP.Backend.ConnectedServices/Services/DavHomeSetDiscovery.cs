using System.Xml.Linq;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;
using ReLiveWP.Dav;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Backend.ConnectedServices.Services;

// RFC 6352 and RFC 4791 both discover a collection the same way: PROPFIND current-user-principal on
// whatever the user typed, then PROPFIND the home set on the principal.
public class DavHomeSetDiscovery(IHttpClientFactory httpClientFactory, IOutboundAddressPolicy addressPolicy)
{
    private static readonly string PrincipalBody = DavBody.Propfind(DavProps.CurrentUserPrincipal);

    public async Task<Uri> DiscoverAsync(
        Uri baseUri, CredentialLink credentials, XName homeSet, string what, CancellationToken ct)
    {
        var principal = await HrefAsync(baseUri, PrincipalBody, DavProps.CurrentUserPrincipal, credentials, ct)
            ?? throw new CredentialLinkException(
                $"The server did not report a current-user-principal; is this a {what} server?");

        var principalUri = new Uri(baseUri, principal);
        addressPolicy.ValidateUri(principalUri);

        var home = await HrefAsync(principalUri, DavBody.Propfind(homeSet), homeSet, credentials, ct)
            ?? throw new CredentialLinkException(
                $"The server did not report a {homeSet.LocalName} for this account.");

        var homeUri = new Uri(principalUri, home);
        addressPolicy.ValidateUri(homeUri);

        return homeUri;
    }

    private async Task<string?> HrefAsync(
        Uri uri, string body, XName property, CredentialLink credentials, CancellationToken ct)
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
