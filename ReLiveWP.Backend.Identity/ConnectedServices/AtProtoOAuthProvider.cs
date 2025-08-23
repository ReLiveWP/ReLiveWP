using System.IdentityModel.Tokens.Jwt;
using System.Resources;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.DPoP;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.App.Bsky.Labeler;
using FishyFlip.Lexicon.Com.Atproto.Repo;
using FishyFlip.Models;
using FishyFlip.Tools;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Backend.Identity.Services;
using static Duende.IdentityModel.OidcConstants;
using Status = Grpc.Core.Status;

namespace ReLiveWP.Backend.Identity.ConnectedServices;

public class ProtectedResourceModel
{
    [JsonPropertyName("authorization_servers")]
    public string[] AuthorizationServers { get; set; } = null!;
}

public class AtProtoOAuthProvider(IClientAssertionService clientAssertionService,
                                  IConnectedServicesContainer connectedServices,
                                  IHttpClientFactory httpClientFactory,
                                  IHttpMessageHandlerFactory httpHandlerFactory,
                                  ILogger<AtProtoOAuthProvider> logger,
                                  ILogger<ATProtocol> atProtoLogger,
                                  IJWKProvider jwkProvider) : IOAuthProvider
{
    public async Task<LivePendingOAuth> BeginAccountLinkAsync(LiveUser user, string handle)
    {
        var description = connectedServices[AtProto.SERVICE_NAME];

        if (handle.StartsWith('@'))
            handle = handle.Substring(1);

        logger.LogInformation("Begin stage 1 linking user {UserId} to @{Handle}", user.Id, handle);

        var state = CryptoRandom.CreateUniqueId();
        var codeVerifier = CryptoRandom.CreateUniqueId(32);
        var codeChallenge = codeVerifier.ToSha256();

        var protocol = new ATProtocolBuilder()
            .Build();

        var atHandle = new ATHandle(handle);
        var (did, _) = (await protocol.ResolveATIdentifierAsync(atHandle))
            .HandleResult();

        logger.LogInformation("Mapped handle @{Handle} to {Did}", atHandle.ToString(), did.ToString());

        var httpClient = httpClientFactory.CreateClient("AtProtoClient");
        var didDoc = (await httpClient.GetDidDocAsync(did))
            .HandleResult() ?? throw new RpcException(new Status(StatusCode.NotFound, "No DID doc was found for the given handle"));

        var pdsUrl = didDoc.GetPDSEndpointUrl()
           ?? throw new RpcException(new Status(StatusCode.NotFound, "No PDS url was specified in the DID doc."));

        logger.LogInformation("Found DID doc for {Did} w/ PDS {PDSUrl}", did, pdsUrl);

        var resourceMetadata = await httpClient.GetFromJsonAsync<ProtectedResourceModel>(new Uri(pdsUrl, "/.well-known/oauth-protected-resource"));
        var authServer = resourceMetadata?.AuthorizationServers.FirstOrDefault()
           ?? throw new RpcException(new Status(StatusCode.NotFound, "No auth server was found."));

        logger.LogInformation("Got auth server {AuthServer} from PDS", authServer);

        var cache = new DiscoveryCache(authServer, new DiscoveryPolicy() { DiscoveryDocumentPath = ".well-known/oauth-authorization-server" });
        var discovery = await cache.GetAsync();

        logger.LogInformation("Got authorization endpoint {Endpoint} from discovery doc", discovery.AuthorizeEndpoint);

        var request = new RequestUrl(discovery.AuthorizeEndpoint!)
            .CreateAuthorizeUrl(
                clientId: description.ClientId,
                responseType: "code",
                scope: "atproto transition:generic",
                redirectUri: description.RedirectUri,
                state: state,
                codeChallenge: codeChallenge,
                codeChallengeMethod: "S256",
                extra: new Parameters() { { AuthorizeRequest.LoginHint, handle, ParameterReplaceBehavior.Single } }
            );

        var pending = new LivePendingOAuth()
        {
            UserId = user.Id,
            State = state,
            Service = "atproto",
            ExpiresAt = DateTimeOffset.Now.AddMinutes(5),

            Endpoint = pdsUrl.ToString(),
            AuthorizationEndpoint = authServer,
            CodeVerifier = codeVerifier,
            RedirectUri = request,
            TokenEndpoint = discovery.TokenEndpoint
        };

        logger.LogInformation("Successfully completed stage 1 account linking for user {UserId} to {DID}", user.Id, did);

        return pending;
    }

    public async Task<LiveConnectedService> FinalizeAccountLinkAsync(LiveConnectedService service, LivePendingOAuth state, string code)
    {
        var description = connectedServices[AtProto.SERVICE_NAME];
        var key = await jwkProvider.GetJWK("Key1");

        logger.LogInformation("Beginning stage 2 account linking for {UserId}", state.UserId);

        var authServer = state.AuthorizationEndpoint!;
        var cache = new DiscoveryCache(authServer, new DiscoveryPolicy() { DiscoveryDocumentPath = ".well-known/oauth-authorization-server" });
        var doc = await cache.GetAsync();

        var tokenString = await clientAssertionService.CreateClientAssertionAsync(description.ClientId, doc.Issuer!);

        var handler = new ProofTokenMessageHandler(key, httpHandlerFactory.CreateHandler("AtProtoClient"));
        var client = new HttpClient(handler);
        var tokenResult = await client.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = doc.TokenEndpoint,

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

        var sub = tokenResult!.Json!.Value!.TryGetValue("sub");
        var subValue = sub!.ToString();

        logger.LogInformation("Successfully completed stage 2 account linking for {UserId} to {DID}", state.UserId, subValue);

        service.Service = AtProto.SERVICE_NAME;
        service.ServiceUrl = state.Endpoint!;
        service.AccessToken = tokenResult.AccessToken!;
        service.RefreshToken = tokenResult.RefreshToken!;
        service.ExpiresAt = DateTimeOffset.Now + TimeSpan.FromSeconds(tokenResult.ExpiresIn);
        service.Flags = LiveConnectedServiceFlags.None;
        service.EnabledCapabilities = LiveConnectedServiceCapabilities.None;
        service.DPoPKeyId = "Key1";
        service.AuthorizationEndpoint = doc.AuthorizeEndpoint;
        service.TokenEndpoint = doc.TokenEndpoint!;
        service.Issuer = doc.Issuer!;

        service.ServiceProfile.UserId = subValue;


        var protocol = new ATProtocolBuilder()
           .EnableAutoRenewSession(false)
           .WithLogger(atProtoLogger)
           .Build();

        var profileView = (await protocol.GetProfileAsync(ATDid.Create(service.ServiceProfile.UserId)!))
             .HandleResult()!;

        service.ServiceProfile.Username = $"@{profileView.Handle}";
        service.ServiceProfile.DisplayName = profileView.DisplayName;
        service.ServiceProfile.AvatarUrl = profileView.Avatar;

        return service;
    }

    public async Task<bool> RefreshTokensAsync(LiveConnectedService service)
    {
        try
        {
            var key = await jwkProvider.GetJWK("Key1");
            var description = connectedServices[AtProto.SERVICE_NAME];

            using var protocol = new ATProtocolBuilder()
               .WithInstanceUrl(new Uri(service.ServiceUrl!))
               .EnableAutoRenewSession(false)
               .WithClientAssertionHandler(async () =>
               {
                   var tokenString = await clientAssertionService.CreateClientAssertionAsync(description.ClientId, service.Issuer!);
                   return new ClientAssertion() { Type = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer", Value = tokenString };
               })
               .WithLogger(atProtoLogger)
               .Build();

            protocol.SessionUpdated += (o, e) =>
            {
                service.AccessToken = e.Session.Session.AccessJwt;
                service.RefreshToken = e.Session.Session.RefreshJwt;
                service.ExpiresAt = e.Session.Session.ExpiresIn;
            };

            var describeRepo = (await protocol.DescribeRepoAsync(ATDid.Create(service.ServiceProfile.UserId)!))
                 .HandleResult()!;

            var session = new Session(describeRepo!.Did!, describeRepo.DidDoc, describeRepo.Handle!, null, service.AccessToken, service.RefreshToken, service.ExpiresAt.DateTime);
            var authSession = new AuthSession(session, key);

            session = (await protocol.AuthenticateWithOAuth2SessionResultAsync(authSession, description.ClientId))
                .HandleResult()!;

            authSession = (await protocol.RefreshAuthSessionResultAsync())
                .HandleResult()!;

            service.AccessToken = authSession.Session.AccessJwt;
            service.RefreshToken = authSession.Session.RefreshJwt;
            service.ExpiresAt = authSession.Session.ExpiresIn;

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh AtProto token.");
            return false;
        }
    }
}
