using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Grpc.Core;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.ConnectedServices.Data;

using static ReLiveWP.Backend.ConnectedServices.OAuthProviders.Microsoft;

namespace ReLiveWP.Backend.ConnectedServices.OAuthProviders;

public class MicrosoftOAuthProvider(IConnectedServicesContainer connectedServices,
                                    IHttpClientFactory httpClientFactory,
                                    ILogger<MicrosoftOAuthProvider> logger) : IOAuthProvider
{
    private readonly ConnectedServiceDescription description = connectedServices[SERVICE_NAME];

    public async Task<LivePendingOAuth> BeginAccountLinkAsync(Guid userId, string identifier)
    {
        var state = CryptoRandom.CreateUniqueId();
        var codeVerifier = CryptoRandom.CreateUniqueId(32);
        var codeChallenge = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier)));

        var discoveryRequest = new DiscoveryDocumentRequest()
        {
            Address = DISCOVERY_URL,
            Policy = new DiscoveryPolicy
            {
                ValidateEndpoints = false,
                ValidateIssuerName = false
            }
        };

        using var httpClient = httpClientFactory.CreateClient();
        var discovery = await httpClient.GetDiscoveryDocumentAsync(discoveryRequest);
        if (discovery.IsError)
            throw new RpcException(new Status(StatusCode.NotFound, "Failed to fetch Microsoft discovery doc!"));

        var request = new RequestUrl(discovery.AuthorizeEndpoint!)
            .CreateAuthorizeUrl(
                clientId: description.ClientId,
                responseType: "code",
                scope: description.Scopes,
                redirectUri: description.RedirectUri,
                state: state,
                codeChallenge: codeChallenge,
                codeChallengeMethod: "S256"
            );

        var pending = new LivePendingOAuth()
        {
            UserId = userId,
            State = state,
            Service = SERVICE_NAME,
            ExpiresAt = DateTimeOffset.Now.AddMinutes(5),
            Endpoint = DISCOVERY_URL,
            AuthorizationEndpoint = discovery.AuthorizeEndpoint,
            CodeVerifier = codeVerifier,
            RedirectUri = request,
            TokenEndpoint = discovery.TokenEndpoint
        };

        return pending;
    }

    public Task<LiveConnectedService> FinalizeAccountLinkAsync(LiveConnectedService service, LivePendingOAuth state, string code)
    {
        throw new NotImplementedException();
    }

    public async Task<LiveConnectedService> FinalizeAccountLinkAsync(LiveConnectedService service, LivePendingOAuth state, string code, string[] scopes)
    {
        using var client = httpClientFactory.CreateClient();
        var tokenResult = await client.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = state.TokenEndpoint,
            ClientId = description.ClientId,
            ClientSecret = description.ClientSecret,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            Code = code,
            RedirectUri = description.RedirectUri,
            CodeVerifier = state.CodeVerifier,
        });

        if (tokenResult.IsError)
            throw new RpcException(new Status(StatusCode.Internal, $"{tokenResult.Error} ({tokenResult.ErrorDescription})"));

        // microsoft doesn't echo `scope` back on the authorize redirect the way google does, so the
        // caller's list is empty on a normal link and the granted scopes have to come off the token.
        var caps = GetCapabilitiesFromScopes(scopes.Length > 0
            ? scopes
            : (tokenResult.Scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));

        service.Service = SERVICE_NAME;
        service.ServiceUrl = state.Endpoint!;
        service.AccessToken = tokenResult.AccessToken!;
        service.RefreshToken = tokenResult.RefreshToken!;
        service.ExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(tokenResult.ExpiresIn);
        service.Flags = LiveConnectedServiceFlags.None;
        service.EnabledCapabilities = 0;
        service.AvailableCapabilities = caps;
        service.AuthorizationEndpoint = state.AuthorizationEndpoint;
        service.TokenEndpoint = state.TokenEndpoint!;

        await FetchUserInfoForService(service);

        return service;
    }

    public async Task<bool> RefreshTokensAsync(LiveConnectedService service)
    {
        try
        {
            using var client = httpClientFactory.CreateClient();
            var result = await client.RequestRefreshTokenAsync(new RefreshTokenRequest()
            {
                Address = service.TokenEndpoint,
                ClientId = description.ClientId,
                ClientSecret = description.ClientSecret,
                RefreshToken = service.RefreshToken,
            });

            if (result.IsError)
                return false;

            service.AccessToken = result.AccessToken!;
            service.RefreshToken = result.RefreshToken ?? service.RefreshToken;
            service.ExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(result.ExpiresIn);

            await FetchUserInfoForService(service);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh Microsoft token.");
            return false;
        }
    }

    private async Task FetchUserInfoForService(LiveConnectedService service)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + service.AccessToken);

        var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me");
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;

        service.ServiceProfile.UserId = root.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
        service.ServiceProfile.DisplayName = root.TryGetProperty("displayName", out var dn) ? dn.GetString() : null;
        service.ServiceProfile.Username = root.TryGetProperty("displayName", out var un) ? un.GetString() : null;
        service.ServiceProfile.EmailAddress = root.TryGetProperty("mail", out var mail) ? mail.GetString()
            : root.TryGetProperty("userPrincipalName", out var upn) ? upn.GetString() : null;
    }

    private static LiveConnectedServiceCapabilities GetCapabilitiesFromScopes(string[] scopes)
    {
        LiveConnectedServiceCapabilities caps = 0;
        foreach (var _scope in scopes)
        {
            var scope = _scope.Trim();
            if (scope.StartsWith("https://graph.microsoft.com/Files."))
                caps |= LiveConnectedServiceCapabilities.FileStorage | LiveConnectedServiceCapabilities.PhotoSync;
        }

        return caps;
    }
}
