using System.Text.Json;
using Duende.IdentityModel;
using Duende.IdentityModel.OidcClient.DPoP;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.Identity.ConnectedServices;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Services.Grpc;

using ServiceCaps = ReLiveWP.Backend.Identity.Data.LiveConnectedServiceCapabilities;

namespace ReLiveWP.Backend.Identity.Services;

public class OAuthService(IConfiguration configuration,
                          IServiceProvider serviceProvider,
                          IConnectedServicesContainer connectedServices,
                          IClientAssertionService clientAssertionService,
                          UserManager<LiveUser> userManager,
                          LiveDbContext dbContext) : ReLiveWP.Services.Grpc.ConnectedServices.ConnectedServicesBase
{
    #region Account Linking

    [Authorize]
    public override async Task<BeginAccountLinkingResponse> BeginAccountLinkingForService(BeginAccountLinkingRequest request, ServerCallContext context)
    {
        var user = await userManager.GetUserAsync(context.GetHttpContext().User)
            ?? throw new RpcException(new Status(StatusCode.Unavailable, "Invalid user was specified."));

        if (!connectedServices.TryGetValue(request.Service, out var serviceDescription))
            throw new RpcException(new Status(StatusCode.Unavailable, "This service is unsupported at this time."));

        using var scope = serviceProvider.CreateScope();

        try
        {
            var serviceHandler = await serviceDescription.OAuthHandler(scope.ServiceProvider); // TODO: get default

            var data = await serviceHandler.BeginAccountLinkAsync(user, request.Identifer);
            await dbContext.PendingOAuths.AddAsync(data);
            await dbContext.SaveChangesAsync();

            return new BeginAccountLinkingResponse()
            {
                RedirectUri = data.RedirectUri
            };
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<FinaliseAccountLinkingResponse> FinaliseAccountLinkingForService(FinaliseAccountLinkingRequest request, ServerCallContext context)
    {
        var pendingOauth = await dbContext.PendingOAuths.FirstOrDefaultAsync(s => s.State == request.State);
        if (pendingOauth == null || pendingOauth.ExpiresAt <= DateTimeOffset.Now)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "This ticket has expired."));

        if (!connectedServices.TryGetValue(pendingOauth.Service, out var serviceDescription))
            throw new RpcException(new Status(StatusCode.Unavailable, "This service is unsupported at this time."));

        var user = await userManager.FindByIdAsync(pendingOauth.UserId.ToString())
            ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "This ticket has expired."));

        dbContext.PendingOAuths.Remove(pendingOauth);

        using var scope = serviceProvider.CreateScope();
        var serviceHandler = await serviceDescription.OAuthHandler(scope.ServiceProvider); // TODO: get default
        var service = new LiveConnectedService()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Service = default!,
            AccessToken = default!,
            RefreshToken = default!,
            ExpiresAt = default!,
            Flags = LiveConnectedServiceFlags.None,
            EnabledCapabilities = serviceDescription.ServiceCapabilities,
        };

        service = await serviceHandler.FinalizeAccountLinkAsync(service, pendingOauth, request.Code);
        service.ServiceProfile = await serviceHandler.GetServiceProfileAsync(service);

        await dbContext.ConnectedServices.AddAsync(service);
        await dbContext.SaveChangesAsync();

        return new FinaliseAccountLinkingResponse();
    }

    #endregion

    public override Task<SupportedConnectionsResponse> GetSupportedConnections(Empty request, ServerCallContext context)
    {
        var response = new SupportedConnectionsResponse();
        foreach (var connection in connectedServices.Values)
        {
            response.AvailableConnections.Add(new SupportedConnection()
            {
                Service = connection.ServiceId,
                DisplayName = connection.DisplayName,
                Capabilities = (ulong)connection.ServiceCapabilities
            });
        }

        return Task.FromResult(response);
    }

    [Authorize]
    public override async Task<ConnectionsResponse> GetConnections(ConnectionsRequest request, ServerCallContext context)
    {
        var user = await userManager.GetUserAsync(context.GetHttpContext().User)
            ?? throw new RpcException(new Status(StatusCode.Unavailable, "Invalid user was specified."));

        var result = new ConnectionsResponse();
        var connections = dbContext.ConnectedServices.Where(c =>
            c.UserId == user.Id &&
            (!request.HasCapabilities || (c.EnabledCapabilities & (ServiceCaps)request.Capabilities) == (ServiceCaps)request.Capabilities) &&
            (request.Services.Count == 0 || request.Services.Contains(c.Service))
        );

        foreach (var item in connections)
        {
            result.Connections.Add(new Connection()
            {
                Id = item.Id.ToString(),
                Service = item.Service,
                ServiceUrl = item.ServiceUrl,
                Capabilities = (ulong)item.EnabledCapabilities,
                Flags = (ulong)item.Flags,

                UserId = item.ServiceProfile.UserId,
                UserName = item.ServiceProfile.Username,
                EmailAddress = item.ServiceProfile.EmailAddress ?? ""
            });
        }

        return result;
    }

    [Authorize]
    public override async Task<ConnectionCredentialsResponse> GetConnectionCredentials(ConnectionCredentialsRequest request, ServerCallContext context)
    {
        var key = configuration["AtProtoOAuth:JWK"]
            ?? throw new RpcException(new Status(StatusCode.Unavailable, "No JsonWebKeys have been configured. This is bad!"));
        var user = await userManager.GetUserAsync(context.GetHttpContext().User)
            ?? throw new RpcException(new Status(StatusCode.Unavailable, "Invalid user was specified."));

        var guid = Guid.Parse(request.ConnectionId);
        var connectedService = await dbContext.ConnectedServices.FirstOrDefaultAsync(s => s.Id == guid);

        // the service can't be used at this time, reject this request
        if (connectedService == null || connectedService.ExpiresAt <= DateTimeOffset.Now || (connectedService.Flags & LiveConnectedServiceFlags.Busted) == LiveConnectedServiceFlags.Busted)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "This ticket has expired."));

        if (!connectedServices.TryGetValue(connectedService.Service, out var serviceDescription))
            throw new RpcException(new Status(StatusCode.Unavailable, "This service is unsupported at this time."));

        var response = new ConnectionCredentialsResponse()
        {
            Credentials = new ConnectionCredential()
            {
                AccessToken = connectedService.AccessToken,
                ClientId = serviceDescription.ClientId,
                ClientSecret = serviceDescription.ClientSecret ?? "",
                ExpiresAt = Timestamp.FromDateTimeOffset(connectedService.ExpiresAt),
                Issuer = connectedService.Issuer ?? serviceDescription.Issuer,
                ServiceUrl = connectedService.ServiceUrl ?? "",
                DpopKey  = key ?? "" // TODO: get rid of this
            }
        };

        return response;
    }


    #region Keys

    public override Task<JsonWebKeysResponse> GetJsonWebKeys(Empty request, ServerCallContext context)
    {
        var key = configuration["AtProtoOAuth:JWK"]
            ?? throw new RpcException(new Status(StatusCode.Unavailable, "No JsonWebKeys have been configured. This is bad!"));

        var webKey = new JsonWebKey(key);
        var publicKey = new JsonWebKey
        {
            Kty = webKey.Kty,
            Crv = webKey.Crv,
            X = webKey.X,
            Y = webKey.Y,
            Kid = "Key1"
        };

        publicKey.KeyOps.Add("sign");
        publicKey.KeyOps.Add("verify");

        var keySet = new JsonWebKeySet();
        keySet.Keys.Add(publicKey);

        var keyString = JsonSerializer.Serialize(keySet);
        return Task.FromResult(new JsonWebKeysResponse() { Keys = keyString });
    }

    [Authorize]
    public override async Task<DPoPProofTokenResponse> GetDPoPProofToken(DPoPProofTokenRequest request, ServerCallContext context)
    {
        var key = configuration["AtProtoOAuth:JWK"]
            ?? throw new RpcException(new Status(StatusCode.Unavailable, "No JsonWebKeys have been configured. This is bad!"));

        var user = await userManager.GetUserAsync(context.GetHttpContext().User)
            ?? throw new RpcException(new Status(StatusCode.Unavailable, "Invalid user was specified."));

        var id = Guid.Parse(request.ConnectionId);
        var connection = await dbContext.ConnectedServices.FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new RpcException(new Status(StatusCode.Unavailable, "Invalid connection."));

        if (!connectedServices.TryGetValue(connection.Service, out var serviceDescription))
            throw new RpcException(new Status(StatusCode.Unavailable, "This service is unsupported at this time."));

        var proofTokenFactory = new DPoPProofTokenFactory(key);
        var proofRequest = new DPoPProofRequest
        {
            Method = request.Method,
            Url = request.Url,
            DPoPNonce = request.Nonce,
            AccessToken = request.AccessToken
        };

        var proof = proofTokenFactory.CreateProofToken(proofRequest);

        return new DPoPProofTokenResponse() { ProofToken = proof.ProofToken };
    }

    [Authorize]
    public override async Task<ClientAssertionResponse> GetClientAssertion(ClientAssertionRequest request, ServerCallContext context)
    {
        var user = await userManager.GetUserAsync(context.GetHttpContext().User)
            ?? throw new RpcException(new Status(StatusCode.Unavailable, "Invalid user was specified."));

        var id = Guid.Parse(request.ConnectionId);
        var connection = await dbContext.ConnectedServices.FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new RpcException(new Status(StatusCode.Unavailable, "Invalid connection."));

        if (!connectedServices.TryGetValue(connection.Service, out var serviceDescription))
            throw new RpcException(new Status(StatusCode.Unavailable, "This service is unsupported at this time."));

        var token = clientAssertionService.CreateClientAssertion(serviceDescription.ClientId, connection.Issuer!); // todo: not everything is going to want these specific parameters
        return new ClientAssertionResponse()
        {
            AssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
            AssertionValue = token
        };
    }

    #endregion
}
