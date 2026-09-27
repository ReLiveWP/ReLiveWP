using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Services;
using ReLiveWP.ServiceDefaults.Outbound;

namespace ReLiveWP.Backend.ConnectedServices.Providers;

public abstract class DavCredentialProviderBase(string serviceName,
                                                string displayName,
                                                IOutboundAddressPolicy addressPolicy,
                                                ConnectionSecretProtector protector,
                                                ILogger logger) : ICredentialLinkProvider
{
    protected string DisplayName { get; } = displayName;

    protected abstract Task<Uri> ResolveCollectionAsync(Uri baseUri, CredentialLink credentials, CancellationToken ct);

    public async Task<LiveConnectedService> LinkAsync(LiveConnectedService connection, CredentialLink credentials,
                                                      CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(credentials.Username) || string.IsNullOrWhiteSpace(credentials.Secret))
            throw new CredentialLinkException("A username and password are required.");

        var baseUri = NormaliseUri(credentials.ServiceUrl);
        addressPolicy.ValidateUri(baseUri);

        var collectionUri = await ResolveCollectionAsync(baseUri, credentials, ct);

        connection.Service = serviceName;
        connection.ServiceUrl = collectionUri.ToString();
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
                ? DescribeShare(collectionUri, credentials.Username)
                : credentials.Label.Trim(),
        };

        logger.LogInformation("Linked {Service} at {CollectionUri} for {Username}", DisplayName, collectionUri, credentials.Username);

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
