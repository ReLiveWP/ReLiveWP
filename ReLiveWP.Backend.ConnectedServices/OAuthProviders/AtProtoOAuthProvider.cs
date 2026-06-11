using System.Text.Json.Serialization;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient.DPoP;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.Com.Atproto.Repo;
using FishyFlip.Models;
using FishyFlip.Tools;
using Grpc.Core;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Services;
using Status = Grpc.Core.Status;
using static Duende.IdentityModel.OidcConstants;

namespace ReLiveWP.Backend.ConnectedServices.OAuthProviders;

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
    public async Task<LivePendingOAuth> BeginAccountLinkAsync(Guid userId, string handle)
    {
        var description = connectedServices[AtProto.SERVICE_NAME];

        while (handle.StartsWith('@'))
            handle = handle[1..];

        logger.LogInformation("Begin stage 1 linking user {UserId} to @{Handle}", userId, handle);

        var state = CryptoRandom.CreateUniqueId();
        var codeVerifier = CryptoRandom.CreateUniqueId(32);
        var codeChallenge = codeVerifier.ToSha256();

        using var protocol = new ATProtocolBuilder()
            .EnableAutoRenewSession(false)
            .WithLogger(atProtoLogger)
            .Build();

        var atHandle = new ATHandle(handle);
        var (did, _) = (await protocol.ResolveATIdentifierAsync(atHandle)).HandleResult();

        logger.LogInformation("Mapped handle @{Handle} to {Did}", atHandle.ToString(), did.ToString());

        using var httpClient = httpClientFactory.CreateClient("AtProtoClient");
        var didDoc = (await httpClient.GetDidDocAsync(did)).HandleResult()
            ?? throw new RpcException(new Status(StatusCode.NotFound, "No DID doc was found for the given handle"));

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
                scope: description.Scopes,
                redirectUri: description.RedirectUri,
                state: state,
                codeChallenge: codeChallenge,
                codeChallengeMethod: "S256",
                extra: new Parameters() { { AuthorizeRequest.LoginHint, handle, ParameterReplaceBehavior.Single } }
            );

        var pending = new LivePendingOAuth()
        {
            UserId = userId,
            State = state,
            Service = "atproto",
            ExpiresAt = DateTimeOffset.Now.AddMinutes(5),
            Endpoint = pdsUrl.ToString(),
            AuthorizationEndpoint = authServer,
            CodeVerifier = codeVerifier,
            RedirectUri = request,
            TokenEndpoint = discovery.TokenEndpoint
        };

        logger.LogInformation("Successfully completed stage 1 account linking for user {UserId} to {DID}", userId, did);

        return pending;
    }

    public async Task<LiveConnectedService> FinalizeAccountLinkAsync(LiveConnectedService service, LivePendingOAuth state, string code)
    {
        var description = connectedServices[AtProto.SERVICE_NAME];
        var (keyId, key) = await jwkProvider.PickKeyAsync();

        logger.LogInformation("Beginning stage 2 account linking for {UserId}", state.UserId);

        var authServer = state.AuthorizationEndpoint!;
        var cache = new DiscoveryCache(authServer, new DiscoveryPolicy() { DiscoveryDocumentPath = ".well-known/oauth-authorization-server" });
        var doc = await cache.GetAsync();

        var tokenString = await clientAssertionService.CreateClientAssertionAsync(description.ClientId, doc.Issuer!, keyId);

        using var handler = new ProofTokenMessageHandler(key, httpHandlerFactory.CreateHandler("AtProtoClient"));
        using var client = new HttpClient(handler);
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
            throw new RpcException(new Status(StatusCode.Internal, $"{tokenResult.Error} ({tokenResult.ErrorDescription})"));

        var subValue = tokenResult.Json!.Value!.TryGetValue("sub")!.ToString();

        logger.LogInformation("Successfully completed stage 2 account linking for {UserId} to {DID}", state.UserId, subValue);

        service.Service = AtProto.SERVICE_NAME;
        service.ServiceUrl = state.Endpoint!;
        service.AccessToken = tokenResult.AccessToken!;
        service.RefreshToken = tokenResult.RefreshToken!;
        service.ExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(tokenResult.ExpiresIn);
        service.Flags = LiveConnectedServiceFlags.None;
        service.EnabledCapabilities = description.ServiceCapabilities;
        service.DPoPKeyId = keyId;
        service.AuthorizationEndpoint = doc.AuthorizeEndpoint;
        service.TokenEndpoint = doc.TokenEndpoint!;
        service.Issuer = doc.Issuer!;
        service.ServiceProfile.UserId = subValue;

        await FetchUserInfoForService(service);

        return service;
    }

    public async Task<bool> RefreshTokensAsync(LiveConnectedService service)
    {
        try
        {
            var key = await jwkProvider.GetJWKAsync(service.DPoPKeyId!);
            var description = connectedServices[AtProto.SERVICE_NAME];

            using var protocol = new ATProtocolBuilder()
               .WithInstanceUrl(new Uri(service.ServiceUrl!))
               .EnableAutoRenewSession(false)
               .WithServiceEndpointUponLogin(false)
               .WithClientAssertionHandler(async () =>
               {
                   var tokenString = await clientAssertionService.CreateClientAssertionAsync(description.ClientId, service.Issuer!, service.DPoPKeyId!);
                   return new ClientAssertion() { Type = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer", Value = tokenString };
               })
               .WithLogger(atProtoLogger)
               .Build();

            protocol.SessionUpdated += (o, e) =>
            {
                service.AccessToken = e.Session.Session.AccessJwt;
                service.RefreshToken = e.Session.Session.RefreshJwt;
                service.ExpiresAt = e.Session.Session.ExpiresIn.ToUniversalTime();
            };

            var describeRepo = (await protocol.DescribeRepoAsync(ATDid.Create(service.ServiceProfile.UserId)!)).HandleResult()!;

            var session = new Session(describeRepo!.Did!, describeRepo.DidDoc, describeRepo.Handle!, null, service.AccessToken, service.RefreshToken, service.ExpiresAt.DateTime);
            var authSession = new AuthSession(session, key);

            session = (await protocol.AuthenticateWithOAuth2SessionResultAsync(authSession, description.ClientId, service.Issuer)).HandleResult()!;
            authSession = (await protocol.RefreshAuthSessionResultAsync()).HandleResult()!;

            service.AccessToken = authSession.Session.AccessJwt;
            service.RefreshToken = authSession.Session.RefreshJwt;
            service.ExpiresAt = authSession.Session.ExpiresIn.ToUniversalTime();
            service.RowVersion++;

            await FetchUserInfoForService(service);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh AT Proto token.");
            return false;
        }
    }

    private async Task FetchUserInfoForService(LiveConnectedService service)
    {
        try
        {
            using var protocol = new ATProtocolBuilder()
               .EnableAutoRenewSession(false)
               .WithLogger(atProtoLogger)
               .Build();

            var profileView = (await protocol.GetProfileAsync(ATDid.Create(service.ServiceProfile.UserId)!)).HandleResult()!;

            service.ServiceProfile.Username = $"@{profileView.Handle}";
            service.ServiceProfile.DisplayName = profileView.DisplayName;
            service.ServiceProfile.AvatarUrl = profileView.Avatar;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Couldn't fetch user information for {ServiceId} ({UserId})", service.Id, service.UserId);
        }
    }
}
