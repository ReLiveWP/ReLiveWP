using System.Net;
using ReLiveWP.Backend.ConnectedServices.Services;
using ReLiveWP.Dav;
using ReLiveWP.ServiceDefaults.Outbound;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Backend.ConnectedServices.Providers;

public class WebDavCredentialProvider(IHttpClientFactory httpClientFactory,
                                      IOutboundAddressPolicy addressPolicy,
                                      ConnectionSecretProtector protector,
                                      ILogger<WebDavCredentialProvider> logger)
    : DavCredentialProviderBase(WebDav.SERVICE_NAME, "WebDAV", addressPolicy, protector, logger)
{
    private static readonly string PropfindBody = DavBody.Propfind(DavProps.ResourceType);

    protected override async Task<Uri> ResolveCollectionAsync(Uri baseUri, CredentialLink credentials, CancellationToken ct)
    {
        using var dav = DavCredentials.CreateClient(httpClientFactory, credentials.Username, credentials.Secret);

        try
        {
            await dav.PropfindAsync(baseUri.ToString(), PropfindBody, depth: "0", ct);
        }
        catch (HttpRequestException ex)
        {
            throw new CredentialLinkException($"Could not reach the server: {ex.Message}");
        }
        catch (DavParseException)
        {
            // a share that answers a PROPFIND at all is a share; the body only has to arrive
        }
        catch (DavException e)
        {
            throw new CredentialLinkException((HttpStatusCode?)e.Status switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                    => "The username or password was rejected by the server.",
                HttpStatusCode.NotFound
                    => "That path doesn't seem to exist on the server.",
                HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotImplemented
                    => "That address doesn't look like a WebDAV share.",
                _ => $"The server refused the connection ({e.Status}).",
            });
        }

        return baseUri;
    }
}
