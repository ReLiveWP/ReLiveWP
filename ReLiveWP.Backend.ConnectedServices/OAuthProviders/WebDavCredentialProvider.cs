using System.Net;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Services;
using ReLiveWP.Dav;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Backend.ConnectedServices.OAuthProviders;

public class WebDavCredentialProvider(IHttpClientFactory httpClientFactory,
                                      IOutboundAddressPolicy addressPolicy,
                                      ConnectionSecretProtector protector,
                                      ILogger<WebDavCredentialProvider> logger) : ICredentialLinkProvider
{
    private static readonly string PropfindBody = DavBody.Propfind(DavProps.ResourceType);

    public async Task<LiveConnectedService> LinkAsync(LiveConnectedService connection, CredentialLink credentials,
                                                      CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(credentials.Username) || string.IsNullOrWhiteSpace(credentials.Secret))
            throw new CredentialLinkException("A username and password are required.");

        var baseUri = NormaliseUri(credentials.ServiceUrl);
        addressPolicy.ValidateUri(baseUri);

        await ProbeAsync(baseUri, credentials, ct);

        connection.Service = WebDav.SERVICE_NAME;
        connection.ServiceUrl = baseUri.ToString();
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
                ? DescribeShare(baseUri, credentials.Username)
                : credentials.Label.Trim(),
        };

        logger.LogInformation("Linked WebDAV share at {Host} for {Username}", baseUri.Host, credentials.Username);

        return connection;
    }
    
    public static string DescribeShare(Uri baseUri, string username)
    {
        var authority = baseUri.IsDefaultPort ? baseUri.Host : $"{baseUri.Host}:{baseUri.Port}";
        var path = baseUri.AbsolutePath.Trim('/');

        return path.Length == 0
            ? $"{username}@{authority}"
            : $"{username}@{authority}/{path}";
    }

    private async Task ProbeAsync(Uri baseUri, CredentialLink credentials, CancellationToken ct)
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
                    => "That path does not exist on the server.",
                HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotImplemented
                    => "That address does not appear to be a WebDAV share.",
                _ => $"The server rejected the connection ({e.Status}).",
            });
        }
    }

    public static Uri NormaliseUri(string serviceUrl)
    {
        if (string.IsNullOrWhiteSpace(serviceUrl))
            throw new CredentialLinkException("A server address is required.");

        var candidate = serviceUrl.Trim();
        if (!candidate.Contains("://", StringComparison.Ordinal))
            candidate = $"https://{candidate}";

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            throw new CredentialLinkException("That server address isn't a valid URL.");

        if (!string.IsNullOrEmpty(uri.UserInfo))
            throw new CredentialLinkException("Put the username and password in the fields below, not in the address.");

        var path = uri.AbsolutePath.EndsWith('/') ? uri.AbsolutePath : uri.AbsolutePath + "/";

        return new UriBuilder(uri.Scheme, uri.Host, uri.IsDefaultPort ? -1 : uri.Port, path).Uri;
    }
}
