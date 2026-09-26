using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Grpc.Core;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Services;
using ReLiveWP.Backend.ConnectedServices.Utilities;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Backend.ConnectedServices.Providers;

public abstract class OidcOAuthProvider(string serviceName,
                                        string discoveryUrl,
                                        IConnectedServicesContainer connectedServices,
                                        IHttpClientFactory httpClientFactory,
                                        ILogger logger) : IOAuthProvider
{
    protected ConnectedServiceDescription Description { get; } = connectedServices[serviceName];

    protected IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;

    protected virtual DiscoveryPolicy DiscoveryPolicy => new() { ValidateEndpoints = false };

    protected virtual Parameters? ExtraAuthorizeParameters => null;

    protected abstract LiveConnectedServiceCapabilities GetGrantedCapabilities(string[] authorizedScopes, TokenResponse tokenResult);

    protected abstract Task FetchUserInfoAsync(LiveConnectedService service);

    public async Task<LivePendingOAuth> BeginAccountLinkAsync(Guid userId, string identifier)
    {
        var state = CryptoRandom.CreateUniqueId();
        var codeVerifier = CryptoRandom.CreateUniqueId(32);
        var codeChallenge = PkceChallenge.CreateS256(codeVerifier);

        var discoveryRequest = new DiscoveryDocumentRequest()
        {
            Address = discoveryUrl,
            Policy = DiscoveryPolicy
        };

        using var httpClient = HttpClientFactory.CreateClient();
        var discovery = await httpClient.GetDiscoveryDocumentAsync(discoveryRequest);
        if (discovery.IsError)
            throw new RpcException(new Status(StatusCode.NotFound, $"Failed to fetch the {Description.DisplayName} discovery doc!"));

        var request = new RequestUrl(discovery.AuthorizeEndpoint!)
            .CreateAuthorizeUrl(
                clientId: Description.ClientId,
                responseType: "code",
                scope: Description.Scopes,
                redirectUri: Description.RedirectUri,
                state: state,
                codeChallenge: codeChallenge,
                codeChallengeMethod: "S256",
                extra: ExtraAuthorizeParameters
            );

        return new LivePendingOAuth()
        {
            UserId = userId,
            State = state,
            Service = serviceName,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            Endpoint = discoveryUrl,
            AuthorizationEndpoint = discovery.AuthorizeEndpoint,
            CodeVerifier = codeVerifier,
            RedirectUri = request,
            TokenEndpoint = discovery.TokenEndpoint
        };
    }

    public Task<LiveConnectedService> FinalizeAccountLinkAsync(LiveConnectedService service, LivePendingOAuth state, string code)
        => FinalizeAccountLinkAsync(service, state, code, []);

    public async Task<LiveConnectedService> FinalizeAccountLinkAsync(LiveConnectedService service, LivePendingOAuth state, string code, string[] authorizedScopes)
    {
        using var client = HttpClientFactory.CreateClient();
        var tokenResult = await client.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = state.TokenEndpoint,
            ClientId = Description.ClientId,
            ClientSecret = Description.ClientSecret,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            Code = code,
            RedirectUri = Description.RedirectUri,
            CodeVerifier = state.CodeVerifier,
        });

        if (tokenResult.IsError)
            throw new RpcException(new Status(StatusCode.Internal, $"{tokenResult.Error} ({tokenResult.ErrorDescription})"));

        var caps = GetGrantedCapabilities(authorizedScopes, tokenResult);

        service.Service = serviceName;
        service.ServiceUrl = state.Endpoint!;
        service.AccessToken = tokenResult.AccessToken!;
        service.RefreshToken = tokenResult.RefreshToken!;
        service.ExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(tokenResult.ExpiresIn);
        service.Flags = LiveConnectedServiceFlags.None;
        service.EnabledCapabilities &= caps;
        service.AvailableCapabilities = caps;
        service.AuthorizationEndpoint = state.AuthorizationEndpoint;
        service.TokenEndpoint = state.TokenEndpoint!;

        await FetchUserInfoAsync(service);

        return service;
    }

    public async Task<bool> RefreshTokensAsync(LiveConnectedService service)
    {
        try
        {
            using var client = HttpClientFactory.CreateClient();
            var result = await client.RequestRefreshTokenAsync(new RefreshTokenRequest()
            {
                Address = service.TokenEndpoint,
                ClientId = Description.ClientId,
                ClientSecret = Description.ClientSecret,
                RefreshToken = service.RefreshToken,
            });

            if (result.IsError)
                return false;

            service.AccessToken = result.AccessToken!;
            service.RefreshToken = result.RefreshToken ?? service.RefreshToken;
            service.ExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(result.ExpiresIn);

            await FetchUserInfoAsync(service);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh {Service} token.", Description.DisplayName);
            return false;
        }
    }

    public async Task RevokeTokensAsync(LiveConnectedService service)
    {
        using var client = HttpClientFactory.CreateClient();

        var discovery = await client.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
        {
            Address = discoveryUrl,
            Policy = DiscoveryPolicy
        });

        if (discovery.IsError || discovery.RevocationEndpoint is not { } revocationEndpoint)
        {
            logger.LogInformation("{Service} has no revocation endpoint, {ConnectionId} is only forgotten locally",
                Description.DisplayName, service.Id);
            return;
        }

        var revokeRefreshToken = !string.IsNullOrEmpty(service.RefreshToken);
        var result = await client.RevokeTokenAsync(new TokenRevocationRequest
        {
            Address = revocationEndpoint,
            ClientId = Description.ClientId,
            ClientSecret = Description.ClientSecret,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            Token = revokeRefreshToken ? service.RefreshToken : service.AccessToken,
            TokenTypeHint = revokeRefreshToken ? OidcConstants.TokenTypes.RefreshToken : OidcConstants.TokenTypes.AccessToken,
        });

        if (result.IsError)
            logger.LogWarning("{Service} refused to revoke the tokens for {ConnectionId}: {Error}",
                Description.DisplayName, service.Id, result.Error);
    }
}
