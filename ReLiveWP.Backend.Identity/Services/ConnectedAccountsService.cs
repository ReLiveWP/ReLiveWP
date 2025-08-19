using System.Text.Json;
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

public class ConnectedAccountsService(IJWKProvider jwkProvider,
                                      IServiceProvider serviceProvider,
                                      IConnectedServicesContainer connectedServices,
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
                //UserName = item.ServiceProfile.Username,
                //EmailAddress = item.ServiceProfile.EmailAddress ?? ""
            });
        }

        return result;
    }

    #region Keys

    public override async Task<JsonWebKeysResponse> GetJsonWebKeys(Empty request, ServerCallContext context)
    {
        var key = await jwkProvider.GetJWK("Key1");

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
        return new JsonWebKeysResponse() { Keys = keyString };
    }

    #endregion
}
