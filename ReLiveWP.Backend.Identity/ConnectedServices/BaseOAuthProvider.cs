using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient.DPoP;
using Grpc.Core;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Backend.Identity.Services;
using ReLiveWP.Identity.Data;

namespace ReLiveWP.Backend.Identity.ConnectedServices;

public abstract class BaseOAuthProvider(string service,
                                        IConnectedServicesContainer connectedServices,
                                        IHttpClientFactory httpClientFactory) : IOAuthProvider
{
    public abstract Task<bool> RefreshTokensAsync(LiveConnectedService connectedService);
    public abstract Task<LiveConnectedServiceProfile> GetServiceProfileAsync(LiveConnectedService connectedService);

    public Task<LivePendingOAuth> BeginAccountLinkAsync(LiveUser user, string _)
    {
        var description = connectedServices[service];

        var state = CryptoRandom.CreateUniqueId();
        var authServer = description.AuthorizationEndpoint
           ?? throw new RpcException(new Status(StatusCode.NotFound, "No auth server was found."));

        var request = new RequestUrl(authServer)
            .CreateAuthorizeUrl(
                clientId: description.ClientId,
                responseType: "code",
                scope: description.Scopes,
                redirectUri: description.RedirectUri,
                state: state
            );

        var pending = new LivePendingOAuth()
        {
            UserId = user.Id,
            State = state,
            Service = "atproto",
            ExpiresAt = DateTimeOffset.Now.AddMinutes(5),
            RedirectUri = request,
            TokenEndpoint = description.TokenEndpoint
        };

        return Task.FromResult(pending);
    }

    public async Task<LiveConnectedService> FinalizeAccountLinkAsync(LiveConnectedService connectedService, LivePendingOAuth state, string code)
    {
        var description = connectedServices[service];

        using var client = httpClientFactory.CreateClient();
        var tokenResult = await client.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = description.TokenEndpoint,

            ClientId = description.ClientId,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,

            Code = code,
            RedirectUri = description.RedirectUri,
            CodeVerifier = state.CodeVerifier,
        });

        if (tokenResult.IsError)
        {
            throw new RpcException(new Status(StatusCode.Internal, $"{tokenResult.Error} ({tokenResult.ErrorDescription})"));
        }

        connectedService.Service = service;
        connectedService.AccessToken = tokenResult.AccessToken!;
        connectedService.RefreshToken = tokenResult.RefreshToken!;
        connectedService.ExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(tokenResult.ExpiresIn);
        connectedService.Flags = LiveConnectedServiceFlags.None;
        connectedService.EnabledCapabilities = LiveConnectedServiceCapabilities.None;
        return connectedService;
    }
}
