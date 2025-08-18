using System.IdentityModel.Tokens.Jwt;
using System.Resources;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.DPoP;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.Com.Atproto.Repo;
using FishyFlip.Models;
using FishyFlip.Tools;
using Grpc.Core;
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
                                  IConfiguration configuration,
                                  LiveDbContext liveDbContext) : IOAuthProvider
{
    public async Task<LivePendingOAuth> BeginAccountLinkAsync(LiveUser user, string identifier)
    {
        var description = connectedServices[AtProto.SERVICE_NAME];

        if (identifier.StartsWith('@'))
            identifier = identifier.Substring(1);

        var state = CryptoRandom.CreateUniqueId();
        var codeVerifier = CryptoRandom.CreateUniqueId(32);
        var codeChallenge = codeVerifier.ToSha256();

        var protocol = new ATProtocolBuilder()
            .Build();
        var atHandle = new ATHandle(identifier);
        var (did, _) = (await protocol.ResolveATIdentifierAsync(atHandle))
            .HandleResult();

        logger.LogInformation("Mapped handle @{Handle} to {Did}", atHandle.ToString(), did.ToString());

        var httpClient = httpClientFactory.CreateClient("AtProtoClient");
        var didDoc = (await httpClient.GetDidDocAsync(did))
            .HandleResult() ?? throw new RpcException(new Status(StatusCode.NotFound, "No DID doc was found for the given handle"));

        var pdsUrl = didDoc.GetPDSEndpointUrl()
           ?? throw new RpcException(new Status(StatusCode.NotFound, "No PDS url was specified in the DID doc."));

        var resourceMetadata = await httpClient.GetFromJsonAsync<ProtectedResourceModel>(new Uri(pdsUrl, "/.well-known/oauth-protected-resource"));

        var authServer = resourceMetadata?.AuthorizationServers.FirstOrDefault()
           ?? throw new RpcException(new Status(StatusCode.NotFound, "No auth server was found."));

        var cache = new DiscoveryCache(authServer, new DiscoveryPolicy() { DiscoveryDocumentPath = ".well-known/oauth-authorization-server" });
        var discovery = await cache.GetAsync();

        var request = new RequestUrl(discovery.AuthorizeEndpoint!)
            .CreateAuthorizeUrl(
                clientId: description.ClientId,
                responseType: "code",
                scope: "atproto transition:generic",
                redirectUri: description.RedirectUri,
                state: state,
                codeChallenge: codeChallenge,
                codeChallengeMethod: "S256",
                extra: new Parameters() { { AuthorizeRequest.LoginHint, identifier, ParameterReplaceBehavior.Single } }
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

        return pending;
    }

    public async Task<LiveConnectedService> FinalizeAccountLinkAsync(LiveConnectedService service, LivePendingOAuth state, string code)
    {
        var description = connectedServices[AtProto.SERVICE_NAME];
        var key = configuration["AtProtoOAuth:JWK"]
                ?? throw new RpcException(new Status(StatusCode.Unavailable, "No JsonWebKeys have been configured. This is bad!"));

        var authServer = state.AuthorizationEndpoint!;
        var cache = new DiscoveryCache(authServer, new DiscoveryPolicy() { DiscoveryDocumentPath = ".well-known/oauth-authorization-server" });
        var doc = await cache.GetAsync();

        var tokenString = clientAssertionService.CreateClientAssertion(description.ClientId, doc.Issuer!);

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

        return service;
    }

    public async Task<bool> RefreshTokensAsync(LiveConnectedService connectedService)
    {
        return false;
    }

    public async Task<LiveConnectedServiceProfile> GetServiceProfileAsync(LiveConnectedService connectedService)
    {
        var description = connectedServices[AtProto.SERVICE_NAME];
        var key = configuration["AtProtoOAuth:JWK"]
                ?? throw new RpcException(new Status(StatusCode.Unavailable, "No JsonWebKeys have been configured. This is bad!"));

        var protocol = new ATProtocolBuilder()
            .WithInstanceUrl(new Uri(connectedService.ServiceUrl!))
            .EnableAutoRenewSession(false)
            .WithLogger(atProtoLogger)
            .Build();

        protocol.SessionUpdated += (o, e) =>
        {
            var service = liveDbContext.ConnectedServices.Find(connectedService.Id);
            if (service == null)
            {
                return;
            }

            service.AccessToken = e.Session.Session.AccessJwt;
            service.RefreshToken = e.Session.Session.RefreshJwt;

            connectedService.AccessToken = e.Session.Session.AccessJwt;
            connectedService.RefreshToken = e.Session.Session.RefreshJwt;

            liveDbContext.SaveChanges();
        };

        var describeRepo = (await protocol.DescribeRepoAsync(ATDid.Create(connectedService.ServiceProfile.UserId)!))
             .HandleResult()!;

        connectedService.ServiceProfile.Username = $"@{describeRepo.Handle}";

        var session = new Session(describeRepo!.Did!, describeRepo.DidDoc, describeRepo.Handle!, null, connectedService.AccessToken, "", connectedService.ExpiresAt.DateTime);
        var authSession = new AuthSession(session, key);

        session = (await protocol.AuthenticateWithOAuth2SessionResultAsync(authSession, description.ClientId))
            .HandleResult()!;

        var profile = (await protocol.GetProfileAsync(session.Did))
            .HandleResult()!;

        connectedService.ServiceProfile.DisplayName = profile.DisplayName;
        connectedService.ServiceProfile.AvatarUrl = profile.Avatar;
        connectedService.ServiceProfile.EmailAddress = session.Email;

        return connectedService.ServiceProfile;
    }
}
