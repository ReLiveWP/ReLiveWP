using System.Net;
using System.Text.Json;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Providers;
using ReLiveWP.ServiceDefaults.Outbound;

namespace ReLiveWP.Backend.ConnectedServices.Services;

public record MastodonClient(
    string ClientId,
    string ClientSecret,
    string RedirectUri,
    string Scopes,
    string AuthorizationEndpoint,
    string TokenEndpoint,
    string? RevocationEndpoint);

public class MastodonClientRegistry(ConnectedServicesDbContext dbContext,
                                    IConnectedServicesContainer connectedServices,
                                    ConnectionSecretProtector protector,
                                    IHttpClientFactory httpClientFactory,
                                    ILogger<MastodonClientRegistry> logger)
{
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromHours(1);

    private readonly ConnectedServiceDescription description = connectedServices[Mastodon.SERVICE_NAME];

    public async Task<MastodonClient> GetOrRegisterClientAsync(Uri instance, CancellationToken ct = default)
    {
        EnsureInstanceRoot(instance);

        var existing = await dbContext.OAuthClients.FindAsync([instance.IdnHost], ct);
        if (existing != null && IsReusable(existing))
            return ToMastodonClient(existing);

        var registered = await RegisterClientAsync(instance, ct);
        var stored = await StoreClientAsync(existing, registered, ct);

        return ToMastodonClient(stored);
    }

    public async Task<MastodonClient?> FindClientAsync(Uri instance, CancellationToken ct = default)
    {
        var existing = await dbContext.OAuthClients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Authority == instance.IdnHost, ct);

        return existing == null ? null : ToMastodonClient(existing);
    }

    public async Task ForgetClientAsync(Uri instance, CancellationToken ct = default)
    {
        logger.LogWarning("Forgetting the app registration for {Authority}", instance.Authority);

        await dbContext.OAuthClients
            .Where(c => c.Authority == instance.IdnHost)
            .ExecuteDeleteAsync(ct);
    }

    private static void EnsureInstanceRoot(Uri instance)
    {
        if (!ExternalRequestGuard.IsAcceptableUri(instance) || instance.AbsolutePath != "/" || instance.Query.Length > 0)
            throw new ArgumentException($"{instance} is not an instance root.", nameof(instance));
    }

    private bool IsReusable(LiveOAuthClient client)
        => client.RedirectUri == description.RedirectUri &&
           client.RequestedScopes == description.Scopes &&
           (client.ExpiresAt is not { } expiresAt || expiresAt - ExpiryMargin > DateTimeOffset.UtcNow);

    private MastodonClient ToMastodonClient(LiveOAuthClient client) => new(
        client.ClientId,
        protector.Unprotect(client.EncryptedSecret),
        client.RedirectUri,
        client.RegisteredScopes,
        client.AuthorizationEndpoint,
        client.TokenEndpoint,
        client.RevocationEndpoint);

    private async Task<LiveOAuthClient> StoreClientAsync(LiveOAuthClient? existing, LiveOAuthClient registered, CancellationToken ct)
    {
        if (existing != null)
        {
            dbContext.Entry(existing).CurrentValues.SetValues(registered);
            await dbContext.SaveChangesAsync(ct);
            return existing;
        }

        dbContext.OAuthClients.Add(registered);
        try
        {
            await dbContext.SaveChangesAsync(ct);
            return registered;
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(registered).State = EntityState.Detached;

            // finalize looks the client up by instance, so a code minted for a registration that lost
            // this race could never be exchanged. theirs wins.
            var winner = await dbContext.OAuthClients.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Authority == registered.Authority, ct);

            if (winner == null)
                throw;

            return winner;
        }
    }

    private async Task<LiveOAuthClient> RegisterClientAsync(Uri instance, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(description.RedirectUri))
            throw new RpcException(new Status(StatusCode.Unavailable, "Mastodon linking has no redirect url configured."));

        using var http = ExternalRequestGuard.CreateGuardedClient(httpClientFactory);

        // nothing gets POSTed to a host until it has shown it actually serves the Mastodon API
        await EnsureSpeaksMastodonApiAsync(http, instance, ct);

        var endpoints = await DiscoverEndpointsAsync(http, instance, ct);

        var scopes = endpoints.SupportsScopes(description.Scopes) ? description.Scopes : Mastodon.FALLBACK_SCOPES;
        var registration = await PostAppRegistrationAsync(http, endpoints.Registration, scopes, ct);

        if (registration == null && scopes != Mastodon.FALLBACK_SCOPES)
        {
            logger.LogInformation("{Authority} refused {Scopes}, registering with {FallbackScopes} instead",
                instance.Authority, scopes, Mastodon.FALLBACK_SCOPES);

            scopes = Mastodon.FALLBACK_SCOPES;
            registration = await PostAppRegistrationAsync(http, endpoints.Registration, scopes, ct);
        }

        if (registration is not { ClientId: { Length: > 0 } clientId, ClientSecret: { Length: > 0 } clientSecret })
            throw new RpcException(new Status(StatusCode.Unavailable, $"{instance.Host} would not register ReLiveWP as an app."));

        logger.LogInformation("Registered ReLiveWP with {Authority} as {ClientId} ({Scopes})", instance.Authority, clientId, scopes);

        return new LiveOAuthClient
        {
            Authority = instance.IdnHost,
            Service = Mastodon.SERVICE_NAME,
            ClientId = clientId,
            EncryptedSecret = protector.Protect(clientSecret),
            RedirectUri = description.RedirectUri,
            RequestedScopes = description.Scopes,
            RegisteredScopes = scopes,
            AuthorizationEndpoint = endpoints.Authorization.AbsoluteUri,
            TokenEndpoint = endpoints.Token.AbsoluteUri,
            RevocationEndpoint = endpoints.Revocation?.AbsoluteUri,
            RegisteredAt = DateTimeOffset.UtcNow,
            ExpiresAt = registration.ClientSecretExpiresAt is long expiresAt and > 0
                ? DateTimeOffset.FromUnixTimeSeconds(expiresAt)
                : null,
        };
    }

    private static async Task EnsureSpeaksMastodonApiAsync(HttpClient http, Uri instance, CancellationToken ct)
    {
        var info = await FetchInstanceInfoAsync(http, new Uri(instance, "/api/v1/instance"), ct)
                   ?? await FetchInstanceInfoAsync(http, new Uri(instance, "/api/v2/instance"), ct);

        if (info is not { Version.Length: > 0 })
            throw new RpcException(new Status(StatusCode.NotFound, $"{instance.Host} doesn't look like a server we can link to."));
    }

    private static async Task<InstanceInfo?> FetchInstanceInfoAsync(HttpClient http, Uri endpoint, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(endpoint, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<InstanceInfo>(FediverseJson.Options, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, $"Could not reach {endpoint.Host}: {ex.Message}"));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<MastodonEndpoints> DiscoverEndpointsAsync(HttpClient http, Uri instance, CancellationToken ct)
    {
        var fixedPaths = new MastodonEndpoints(
            new Uri(instance, "/oauth/authorize"),
            new Uri(instance, "/oauth/token"),
            new Uri(instance, "/oauth/revoke"),
            new Uri(instance, "/api/v1/apps"),
            ScopesSupported: null);

        AuthorizationServerMetadata? metadata;
        try
        {
            using var response = await http.GetAsync(new Uri(instance, "/.well-known/oauth-authorization-server"), ct);
            if (!response.IsSuccessStatusCode)
                return fixedPaths;

            metadata = await response.Content.ReadFromJsonAsync<AuthorizationServerMetadata>(FediverseJson.Options, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, $"Could not reach {instance.Host}: {ex.Message}"));
        }
        catch (JsonException)
        {
            return fixedPaths;
        }

        if (metadata == null)
            return fixedPaths;

        return new MastodonEndpoints(
            ResolveEndpoint(instance, metadata.AuthorizationEndpoint) ?? fixedPaths.Authorization,
            ResolveEndpoint(instance, metadata.TokenEndpoint) ?? fixedPaths.Token,
            ResolveEndpoint(instance, metadata.RevocationEndpoint),
            ResolveEndpoint(instance, metadata.AppRegistrationEndpoint) ?? fixedPaths.Registration,
            metadata.ScopesSupported);
    }

    private static Uri? ResolveEndpoint(Uri instance, string? endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            return null;

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || !ExternalRequestGuard.IsOnInstance(uri, instance))
            throw new RpcException(new Status(StatusCode.FailedPrecondition,
                $"{instance.Host} advertises an OAuth endpoint that isn't on {instance.Host}, refusing to use it."));

        return uri;
    }

    private async Task<AppRegistration?> PostAppRegistrationAsync(HttpClient http, Uri endpoint, string scopes, CancellationToken ct)
    {
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_name"] = Mastodon.CLIENT_NAME,
            ["redirect_uris"] = description.RedirectUri,
            ["scopes"] = scopes,
            ["website"] = Mastodon.CLIENT_WEBSITE,
        });

        try
        {
            using var response = await http.PostAsync(endpoint, form, ct);

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
                return null;

            if (!response.IsSuccessStatusCode)
                throw new RpcException(new Status(StatusCode.Unavailable,
                    $"{endpoint.Host} refused the app registration ({(int)response.StatusCode})."));

            return await response.Content.ReadFromJsonAsync<AppRegistration>(FediverseJson.Options, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, $"Could not reach {endpoint.Host}: {ex.Message}"));
        }
        catch (JsonException)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, $"{endpoint.Host} sent back an app registration we could not read."));
        }
    }

    private sealed record MastodonEndpoints(Uri Authorization, Uri Token, Uri? Revocation, Uri Registration, string[]? ScopesSupported)
    {
        public bool SupportsScopes(string scopes)
            => ScopesSupported == null || scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(ScopesSupported.Contains);
    }

    private sealed record AuthorizationServerMetadata(
        string? AuthorizationEndpoint,
        string? TokenEndpoint,
        string? RevocationEndpoint,
        string? AppRegistrationEndpoint,
        string[]? ScopesSupported);

    private sealed record AppRegistration(string? ClientId, string? ClientSecret, long? ClientSecretExpiresAt);

    private sealed record InstanceInfo(string? Version);
}
