using System.Security.Claims;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;
using ReLiveWP.Services.Grpc;
namespace ReLiveWP.Backend.ConnectedServices.Grpc;

public class ConnectedAccountsService(IServiceProvider serviceProvider,
                                      IConnectedServicesContainer connectedServices,
                                      ConnectedServicesDbContext dbContext) : ReLiveWP.Services.Grpc.ConnectedServices.ConnectedServicesBase
{
    #region Account Linking

    [Authorize]
    public override async Task<BeginAccountLinkingResponse> BeginAccountLinkingForService(BeginAccountLinkingRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!connectedServices.TryGetValue(request.Service, out var serviceDescription))
            throw new RpcException(new Status(StatusCode.Unavailable, "This service is unsupported at this time."));

        using var scope = serviceProvider.CreateScope();

        try
        {
            var handler = await serviceDescription.OAuthHandler(scope.ServiceProvider);
            var data = await handler.BeginAccountLinkAsync(userId, request.Identifer);
            await dbContext.PendingOAuths.AddAsync(data);
            await dbContext.SaveChangesAsync();

            return new BeginAccountLinkingResponse() { RedirectUri = data.RedirectUri };
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

        dbContext.PendingOAuths.Remove(pendingOauth);

        using var scope = serviceProvider.CreateScope();
        var handler = await serviceDescription.OAuthHandler(scope.ServiceProvider);

        if (pendingOauth.ExistingConnectionId is Guid existingId)
        {
            var existing = await dbContext.ConnectedServices.FindAsync(existingId)
                ?? throw new RpcException(new Status(StatusCode.NotFound, "Connection no longer exists."));

            if (existing.UserId != pendingOauth.UserId)
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Connection does not belong to this user."));

            await handler.FinalizeAccountLinkAsync(existing, pendingOauth, request.Code);
            dbContext.ConnectedServices.Update(existing);
        }
        else
        {
            var service = new LiveConnectedService()
            {
                Id = Guid.NewGuid(),
                UserId = pendingOauth.UserId,
                Service = default!,
                AccessToken = default!,
                RefreshToken = default!,
                ExpiresAt = default!,
                Flags = LiveConnectedServiceFlags.None,
                EnabledCapabilities = serviceDescription.ServiceCapabilities,
            };

            service = await handler.FinalizeAccountLinkAsync(service, pendingOauth, request.Code);
            await dbContext.ConnectedServices.AddAsync(service);
        }

        await dbContext.SaveChangesAsync();

        return new FinaliseAccountLinkingResponse();
    }

    [Authorize]
    public override async Task<BeginAccountLinkingResponse> BeginRelinkForConnection(BeginRelinkRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.ConnectionId, out var connectionId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid connection ID."));

        var existing = await dbContext.ConnectedServices.FindAsync(connectionId)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Connection not found."));

        if (existing.UserId != userId)
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Connection does not belong to this user."));

        if (!connectedServices.TryGetValue(existing.Service, out var serviceDescription))
            throw new RpcException(new Status(StatusCode.Unavailable, "This service is unsupported at this time."));

        // Use the stored handle (strip leading @) so handle→PDS resolution runs the same as initial link.
        var identifier = existing.ServiceProfile.Username?.TrimStart('@')
            ?? existing.ServiceProfile.UserId;

        using var scope = serviceProvider.CreateScope();
        var handler = await serviceDescription.OAuthHandler(scope.ServiceProvider);
        var data = await handler.BeginAccountLinkAsync(userId, identifier);
        data.ExistingConnectionId = connectionId;

        await dbContext.PendingOAuths.AddAsync(data);
        await dbContext.SaveChangesAsync();

        return new BeginAccountLinkingResponse() { RedirectUri = data.RedirectUri };
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
    public override async Task GetConnections(ConnectionsRequest request, IServerStreamWriter<Connection> responseStream, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var connections = dbContext.ConnectedServices.Where(c =>
            c.UserId == userId &&
            (!request.HasCapabilities || (c.EnabledCapabilities & (LiveConnectedServiceCapabilities)request.Capabilities) == (LiveConnectedServiceCapabilities)request.Capabilities) &&
            (request.Services.Count == 0 || request.Services.Contains(c.Service))
        ).AsAsyncEnumerable();

        await foreach (var item in connections)
        {
            await responseStream.WriteAsync(new Connection()
            {
                Id = item.Id.ToString(),
                Service = item.Service,
                ServiceUrl = item.ServiceUrl,
                Capabilities = (ulong)item.EnabledCapabilities,
                Flags = (ulong)item.Flags,
                UserId = item.ServiceProfile.UserId,
                UserName = item.ServiceProfile.Username,
            });
        }
    }

    public override async Task<DeleteConnectionResponse> DeleteConnection(DeleteConnectionRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        var connId = Guid.Parse(request.ConnectionId);
        var connection = await dbContext.ConnectedServices.FirstOrDefaultAsync(r => r.UserId == userId && r.Id == connId);
        if (connection == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Connection not found!"));
        }

        dbContext.ConnectedServices.Remove(connection);
        await dbContext.SaveChangesAsync();

        return new DeleteConnectionResponse();
    }

    #region Keys

    public override async Task<JsonWebKeysResponse> GetJsonWebKeys(Empty request, ServerCallContext context)
    {
        var keySet = new JsonWebKeySet();

        await foreach (var item in dbContext.DPoPKeys)
        {
            var webKey = new JsonWebKey(item.Key);
            var publicKey = new JsonWebKey
            {
                Kty = webKey.Kty,
                Crv = webKey.Crv,
                X = webKey.X,
                Y = webKey.Y,
                Kid = webKey.KeyId
            };
            publicKey.KeyOps.Add("verify");
            keySet.Keys.Add(publicKey);
        }

        return new JsonWebKeysResponse() { Keys = JsonSerializer.Serialize(keySet) };
    }

    #endregion

    private static Guid GetUserId(ServerCallContext context)
    {
        var sub = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid user."));
        return Guid.Parse(sub);
    }
}
