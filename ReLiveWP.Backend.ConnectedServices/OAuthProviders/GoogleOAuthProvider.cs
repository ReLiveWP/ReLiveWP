using System.Security.Cryptography;
using System.Text;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Grpc.Core;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.ConnectedServices.Data;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

using static ReLiveWP.Backend.ConnectedServices.OAuthProviders.Google;

namespace ReLiveWP.Backend.ConnectedServices.OAuthProviders;

public class GoogleOAuthProvider(IConnectedServicesContainer connectedServices,
                                 IHttpClientFactory httpClientFactory,
                                 ILogger<GoogleOAuthProvider> logger) : IOAuthProvider
{
    private readonly ConnectedServiceDescription description = connectedServices[SERVICE_NAME];

    public Task<LivePendingOAuth> BeginAccountLinkAsync(Guid userId, string identifier) =>
        BeginAccountLinkAsync(userId, identifier, LiveConnectedServiceCapabilities.None);

    public async Task<LivePendingOAuth> BeginAccountLinkAsync(
        Guid userId, string identifier, LiveConnectedServiceCapabilities requested)
    {
        var state = CryptoRandom.CreateUniqueId();
        var codeVerifier = CryptoRandom.CreateUniqueId(32);
        var codeChallenge = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier))); // remove base64 padding

        var discoveryRequest = new DiscoveryDocumentRequest()
        {
            Address = DISCOVERY_URL,
            Policy = new DiscoveryPolicy
            {
                ValidateEndpoints = false
            }
        };

        using var httpClient = httpClientFactory.CreateClient();
        var discovery = await httpClient.GetDiscoveryDocumentAsync(discoveryRequest);
        if (discovery.IsError)
            throw new RpcException(new Status(StatusCode.NotFound, "Failed to fetch discovery doc!"));

        var request = new RequestUrl(discovery.AuthorizeEndpoint!)
            .CreateAuthorizeUrl(
                clientId: description.ClientId,
                responseType: "code",
                scope: description.Scopes,
                redirectUri: description.RedirectUri,
                state: state,
                codeChallenge: codeChallenge,
                codeChallengeMethod: "S256",
                extra: new Parameters() { { "access_type", "offline" }, { "prompt", "consent" } }
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
        // google requires scopes
        throw new NotImplementedException();
    }

    public async Task<LiveConnectedService> FinalizeAccountLinkAsync(LiveConnectedService service, LivePendingOAuth state, string code, string[] scopes)
    {
        var caps = GetCapabilitiesFromScopes(scopes);

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

        var subValue = tokenResult.Json!.Value!.TryGetValue("sub")!.ToString();

        service.Service = SERVICE_NAME;
        service.ServiceUrl = state.Endpoint!;
        service.AccessToken = tokenResult.AccessToken!;
        service.RefreshToken = tokenResult.RefreshToken!;
        service.ExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(tokenResult.ExpiresIn);
        service.Flags = LiveConnectedServiceFlags.None;
        service.EnabledCapabilities &= caps;
        service.AvailableCapabilities = caps;
        service.AuthorizationEndpoint = state.AuthorizationEndpoint;
        service.TokenEndpoint = state.TokenEndpoint!;

        service.ServiceProfile.UserId = subValue;

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
            service.ExpiresAt = DateTimeOffset.Now + TimeSpan.FromSeconds(result.ExpiresIn);

            await FetchUserInfoForService(service);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh Google token.");
            return false;
        }
    }

    private async Task FetchUserInfoForService(LiveConnectedService service)
    {
        var oauthService = new Oauth2Service(new BaseClientService.Initializer()
        {
            ApplicationName = "ReLiveWP Connected Services"
        });

        oauthService.HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + service.AccessToken);

        var response = await oauthService.Userinfo.Get()
            .ExecuteAsync();

        service.ServiceProfile.UserId = response.Id;
        service.ServiceProfile.Username = response.Name;
        service.ServiceProfile.DisplayName = response.Name;
        service.ServiceProfile.AvatarUrl = response.Picture;
        service.ServiceProfile.EmailAddress = response.Email;
    }

    private const string ScopePrefix = "https://www.googleapis.com/auth/";

    internal static LiveConnectedServiceCapabilities GetCapabilitiesFromScopes(string[] scopes)
    {
        LiveConnectedServiceCapabilities caps = 0;

        foreach (var raw in scopes)
        {
            var scope = raw.Trim();
            if (!scope.StartsWith(ScopePrefix, StringComparison.Ordinal)) continue;

            var name = scope[ScopePrefix.Length..];

            caps |= name.Split('.')[0] switch
            {
                "contacts" => LiveConnectedServiceCapabilities.Contacts,
                "calendar" => LiveConnectedServiceCapabilities.Calendar,
                "drive" => LiveConnectedServiceCapabilities.FileStorage,
                "photoslibrary" => LiveConnectedServiceCapabilities.PhotoSync,
                "gmail" => LiveConnectedServiceCapabilities.Email,
                _ => 0,
            };
        }

        return caps;
    }
}
