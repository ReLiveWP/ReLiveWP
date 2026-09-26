using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Grpc.Core;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Services;
using ReLiveWP.Backend.ConnectedServices.Utilities;
using ReLiveWP.ServiceDefaults.Outbound;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

using static ReLiveWP.Backend.ConnectedServices.Providers.Mastodon;

namespace ReLiveWP.Backend.ConnectedServices.Providers;

public class MastodonOAuthProvider(MastodonClientRegistry clientRegistry,
                                   IHttpClientFactory httpClientFactory,
                                   ILogger<MastodonOAuthProvider> logger) : IOAuthProvider
{
    private const int MaxDisplayNameLength = 200;

    public async Task<LivePendingOAuth> BeginAccountLinkAsync(Guid userId, string identifier)
    {
        if (!FediverseHandle.TryParse(identifier, out var handle))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "That doesn't look like a fediverse handle."));

        var instance = await ResolveInstanceAsync(handle);
        var client = await clientRegistry.GetOrRegisterClientAsync(instance);

        var state = CryptoRandom.CreateUniqueId();
        var codeVerifier = CryptoRandom.CreateUniqueId(32);
        var codeChallenge = PkceChallenge.CreateS256(codeVerifier);

        var authorizeUrl = new RequestUrl(client.AuthorizationEndpoint)
            .CreateAuthorizeUrl(
                clientId: client.ClientId,
                responseType: "code",
                scope: client.Scopes,
                redirectUri: client.RedirectUri,
                state: state,
                codeChallenge: codeChallenge,
                codeChallengeMethod: "S256");

        logger.LogInformation("Linking {UserId} to an account on {Instance}", userId, instance.Authority);

        return new LivePendingOAuth()
        {
            UserId = userId,
            State = state,
            Service = SERVICE_NAME,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            Endpoint = instance.AbsoluteUri,
            AuthorizationEndpoint = client.AuthorizationEndpoint,
            TokenEndpoint = client.TokenEndpoint,
            CodeVerifier = codeVerifier,
            RedirectUri = authorizeUrl,
        };
    }

    public async Task<LiveConnectedService> FinalizeAccountLinkAsync(LiveConnectedService service, LivePendingOAuth state, string code)
    {
        if (!FediverseRequestGuard.TryParseAcceptableUri(state.Endpoint, out var instance))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "This link request doesn't name a usable server."));

        var client = await clientRegistry.FindClientAsync(instance)
            ?? throw new RpcException(new Status(StatusCode.FailedPrecondition,
                $"ReLiveWP's registration with {instance.Host} went away, try linking again."));

        if (!Uri.TryCreate(client.TokenEndpoint, UriKind.Absolute, out var tokenEndpoint) ||
            !FediverseRequestGuard.IsOnInstance(tokenEndpoint, instance))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"The token endpoint for {instance.Host} isn't on {instance.Host}."));

        using var http = FediverseRequestGuard.CreateGuardedClient(httpClientFactory);

        var tokenResult = await http.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = tokenEndpoint.AbsoluteUri,
            ClientId = client.ClientId,
            ClientSecret = client.ClientSecret,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            Code = code,
            RedirectUri = client.RedirectUri,
            CodeVerifier = state.CodeVerifier,
            Parameters = new Parameters { { OidcConstants.TokenRequest.Scope, client.Scopes } },
        });

        if (tokenResult.IsError)
        {
            // the instance has forgotten us (expired secret, or an admin deleted the app), so the next link re-registers
            if (tokenResult.Error == OidcConstants.TokenErrors.InvalidClient || tokenResult.HttpStatusCode == HttpStatusCode.Unauthorized)
                await clientRegistry.ForgetClientAsync(instance);

            throw new RpcException(new Status(StatusCode.Internal, $"{tokenResult.Error} ({tokenResult.ErrorDescription})"));
        }

        var accessToken = tokenResult.AccessToken
            ?? throw new RpcException(new Status(StatusCode.Internal, $"{instance.Host} didn't hand back an access token."));

        var account = await FetchAccountAsync(http, instance, accessToken)
            ?? throw new RpcException(new Status(StatusCode.Internal, $"{instance.Host} didn't tell us which account was linked."));

        var actorUri = await ResolveActorUriAsync(http, instance, account)
            ?? throw new RpcException(new Status(StatusCode.Internal,
                $"Couldn't work out the ActivityPub id of @{account.Username} on {instance.Host}."));

        var grantedScopes = (tokenResult.Scope ?? client.Scopes).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var caps = GetCapabilitiesFromScopes(grantedScopes);

        service.Service = SERVICE_NAME;
        service.ServiceUrl = instance.AbsoluteUri;
        service.AccessToken = accessToken;
        service.RefreshToken = "";
        service.ExpiresAt = DateTimeOffset.MaxValue;
        service.Flags = LiveConnectedServiceFlags.None;
        service.EnabledCapabilities &= caps;
        service.AvailableCapabilities = caps;
        service.AuthorizationEndpoint = client.AuthorizationEndpoint;
        service.TokenEndpoint = client.TokenEndpoint;
        service.ServiceProfile.UserId = actorUri.AbsoluteUri;
        ApplyAccountProfile(service, account, instance);

        logger.LogInformation("Linked {UserId} to {ActorUri}", state.UserId, actorUri);

        return service;
    }

    public async Task<bool> RefreshTokensAsync(LiveConnectedService service)
    {
        if (!FediverseRequestGuard.TryParseAcceptableUri(service.ServiceUrl, out var instance))
        {
            logger.LogError("{ConnectionId} has no usable instance url", service.Id);
            return false;
        }

        try
        {
            using var http = FediverseRequestGuard.CreateGuardedClient(httpClientFactory);
            using var response = await SendVerifyCredentialsAsync(http, instance, service.AccessToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                logger.LogWarning("{Instance} rejected the token for {ConnectionId}", instance.Authority, service.Id);
                return false;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("{Instance} returned {Status} checking {ConnectionId}, leaving it linked",
                    instance.Authority, (int)response.StatusCode, service.Id);
                return true;
            }

            var account = await response.Content.ReadFromJsonAsync<MastodonAccount>(FediverseJson.Options);
            if (account is { Username.Length: > 0 })
                ApplyAccountProfile(service, account, instance);

            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Couldn't reach {Instance} to check {ConnectionId}, leaving it linked", instance.Authority, service.Id);
            return true;
        }
    }

    public async Task RevokeTokensAsync(LiveConnectedService service)
    {
        if (!FediverseRequestGuard.TryParseAcceptableUri(service.ServiceUrl, out var instance))
            return;

        var client = await clientRegistry.FindClientAsync(instance);
        if (client == null)
        {
            logger.LogInformation("No app registration for {Instance}, {ConnectionId} is only forgotten locally", instance.Authority, service.Id);
            return;
        }

        if (!Uri.TryCreate(client.RevocationEndpoint, UriKind.Absolute, out var revocationEndpoint))
            revocationEndpoint = new Uri(instance, "/oauth/revoke");

        if (!FediverseRequestGuard.IsOnInstance(revocationEndpoint, instance))
            return;

        using var http = FediverseRequestGuard.CreateGuardedClient(httpClientFactory);
        var result = await http.RevokeTokenAsync(new TokenRevocationRequest
        {
            Address = revocationEndpoint.AbsoluteUri,
            ClientId = client.ClientId,
            ClientSecret = client.ClientSecret,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            Token = service.AccessToken,
        });

        if (result.IsError)
            logger.LogWarning("{Instance} refused to revoke the token for {ConnectionId}: {Error}", instance.Authority, service.Id, result.Error);
    }

    public static LiveConnectedServiceCapabilities GetCapabilitiesFromScopes(IEnumerable<string> scopes)
    {
        var caps = LiveConnectedServiceCapabilities.None;
        foreach (var scope in scopes)
        {
            caps |= scope switch
            {
                "read" or "read:statuses" => LiveConnectedServiceCapabilities.SocialFeed | LiveConnectedServiceCapabilities.SocialPhotos,
                "write" or "write:statuses" => LiveConnectedServiceCapabilities.SocialPost,
                _ => LiveConnectedServiceCapabilities.None,
            };
        }

        return caps;
    }

    private async Task<Uri> ResolveInstanceAsync(FediverseHandle handle)
    {
        if (handle.AccountAddress is not { } address)
            return handle.DomainRoot;

        using var http = FediverseRequestGuard.CreateGuardedClient(httpClientFactory);

        // split-domain servers answer webfinger on the handle's domain but serve the API from the actor's
        var actorUri = await FediverseWebFinger.FindActorUriAsync(http, address);
        return actorUri == null ? handle.DomainRoot : FediverseRequestGuard.GetInstanceRoot(actorUri);
    }

    private async Task<Uri?> ResolveActorUriAsync(HttpClient http, Uri instance, MastodonAccount account)
    {
        if (account.Uri != null)
        {
            if (Uri.TryCreate(account.Uri, UriKind.Absolute, out var claimed) && FediverseRequestGuard.IsOnInstance(claimed, instance))
                return claimed;

            logger.LogWarning("{Instance} claimed an actor off-instance ({ActorUri}), ignoring it", instance.Authority, account.Uri);
            return null;
        }

        var discovered = await FediverseWebFinger.FindActorUriAsync(http, $"{account.Username}@{instance.IdnHost}");
        if (discovered != null && FediverseRequestGuard.IsOnInstance(discovered, instance))
            return discovered;

        return null;
    }

    private static async Task<MastodonAccount?> FetchAccountAsync(HttpClient http, Uri instance, string accessToken)
    {
        using var response = await SendVerifyCredentialsAsync(http, instance, accessToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var account = await response.Content.ReadFromJsonAsync<MastodonAccount>(FediverseJson.Options);
        return account is { Username.Length: > 0 } ? account : null;
    }

    private static async Task<HttpResponseMessage> SendVerifyCredentialsAsync(HttpClient http, Uri instance, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(instance, "/api/v1/accounts/verify_credentials"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await http.SendAsync(request);
    }

    private static void ApplyAccountProfile(LiveConnectedService service, MastodonAccount account, Uri instance)
    {
        var displayName = string.IsNullOrWhiteSpace(account.DisplayName) ? account.Username : account.DisplayName.Trim();

        var accountAddress = FediverseHandle.DescribeAccountAddress(account.Username, account.Fqn, acct: null, instance);

        service.ServiceProfile.Username = $"@{accountAddress}";
        service.ServiceProfile.DisplayName = displayName.Length > MaxDisplayNameLength ? displayName[..MaxDisplayNameLength] : displayName;
        service.ServiceProfile.AvatarUrl = FediverseRequestGuard.TryParseAcceptableUri(account.Avatar, out var avatar) ? avatar.AbsoluteUri : null;
    }

    private sealed record MastodonAccount(string Username, string? DisplayName, string? Avatar, string? Uri, string? Fqn);
}
