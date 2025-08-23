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
                                        IJWKProvider jwkProvider) : IOAuthProvider
{
    public Task<LivePendingOAuth> BeginAccountLinkAsync(LiveUser user, string _)
    {
        var description = connectedServices[service];

        var state = CryptoRandom.CreateUniqueId();
        var codeVerifier = CryptoRandom.CreateUniqueId(32);
        var codeChallenge = codeVerifier.ToSha256();

        var authServer = description.AuthorizationEndpoint
           ?? throw new RpcException(new Status(StatusCode.NotFound, "No auth server was found."));

        var request = new RequestUrl(authServer)
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
            UserId = user.Id,
            State = state,
            Service = "atproto",
            ExpiresAt = DateTimeOffset.Now.AddMinutes(5),
            CodeVerifier = codeVerifier,
            RedirectUri = request,
            TokenEndpoint = description.TokenEndpoint
        };

        return Task.FromResult(pending);
    }

    public abstract Task<bool> RefreshTokensAsync(LiveConnectedService connectedService);
    public abstract Task<LiveConnectedServiceProfile> GetServiceProfileAsync(LiveConnectedService connectedService);

    public async Task<LiveConnectedService> FinalizeAccountLinkAsync(LiveConnectedService connectedService, LivePendingOAuth state, string code)
    {
        var description = connectedServices[service];
        var key = await jwkProvider.GetJWK("Key1");

        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expiresAt = issuedAt + 300;

        var authClaims = new List<Claim>()
        {
            new Claim(JwtRegisteredClaimNames.Iss, description.ClientId),
            new Claim(JwtRegisteredClaimNames.Sub, description.ClientId),
            new Claim(JwtRegisteredClaimNames.Aud, description.Issuer!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, issuedAt.ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp, expiresAt.ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            expires: DateTimeOffset.Now.AddMinutes(5).UtcDateTime,
            claims: authClaims,
            signingCredentials: new SigningCredentials(new JsonWebKey(key) { KeyId = "Key1" }, SecurityAlgorithms.EcdsaSha256)
        );

        var tokenString = new JwtSecurityTokenHandler()
            .WriteToken(token);

        var handler = new ProofTokenMessageHandler(key, new SocketsHttpHandler());
        var client = new HttpClient(handler);
        var tokenResult = await client.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = description.TokenEndpoint,

            ClientId = description.ClientId,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            ClientAssertion = new ClientAssertion() { Type = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer", Value = tokenString },

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
        connectedService.ExpiresAt = DateTimeOffset.Now + TimeSpan.FromSeconds(tokenResult.ExpiresIn);
        connectedService.Flags = LiveConnectedServiceFlags.None;
        connectedService.EnabledCapabilities = LiveConnectedServiceCapabilities.None;
        connectedService.DPoPKeyId = "Key1";
        return connectedService;
    }
}
