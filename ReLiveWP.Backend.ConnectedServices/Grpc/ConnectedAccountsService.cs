using System.Security.Claims;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;
using ReLiveWP.Backend.ConnectedServices.Services;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.ConnectedServices.Grpc;

public class ConnectedAccountsService(IServiceProvider serviceProvider,
                                      IConnectedServicesContainer connectedServices,
                                      PendingOAuthStore pendingOAuths,
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
            var handler = await ResolveOAuthHandlerAsync(serviceDescription, scope.ServiceProvider);
            var data = await handler.BeginAccountLinkAsync(userId, request.Identifer);
            await pendingOAuths.SetAsync(data);

            return new BeginAccountLinkingResponse() { RedirectUri = data.RedirectUri };
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<FinaliseAccountLinkingResponse> FinaliseAccountLinkingForService(FinaliseAccountLinkingRequest request, ServerCallContext context)
    {
        var pendingOauth = await pendingOAuths.GetAsync(request.State);
        if (pendingOauth == null || pendingOauth.ExpiresAt <= DateTimeOffset.Now)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "This ticket has expired."));

        if (!connectedServices.TryGetValue(pendingOauth.Service, out var serviceDescription))
            throw new RpcException(new Status(StatusCode.Unavailable, "This service is unsupported at this time."));

        using var scope = serviceProvider.CreateScope();

        var handler = await ResolveOAuthHandlerAsync(serviceDescription, scope.ServiceProvider);

        Guid? serviceId;
        if (pendingOauth.ExistingConnectionId is Guid existingId)
        {
            var existing = await dbContext.ConnectedServices.FindAsync(existingId)
                ?? throw new RpcException(new Status(StatusCode.NotFound, "Connection no longer exists."));

            if (existing.UserId != pendingOauth.UserId)
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Connection does not belong to this user."));

            // technically this should mutate what is in `existing` but it's nice to be explicit about it
            existing = await handler.FinalizeAccountLinkAsync(existing, pendingOauth, request.Code, [.. request.Scopes]);
            dbContext.ConnectedServices.Update(existing);

            serviceId = existing.Id;
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
                AvailableCapabilities = serviceDescription.ServiceCapabilities,
                EnabledCapabilities = 0
            };

            // ditto for `service`
            service = await handler.FinalizeAccountLinkAsync(service, pendingOauth, request.Code, [.. request.Scopes]);
            await dbContext.ConnectedServices.AddAsync(service);

            serviceId = service.Id;
        }

        await dbContext.SaveChangesAsync();
        await pendingOAuths.RemoveAsync(pendingOauth.State); // consume the one-shot ticket on success

        return new FinaliseAccountLinkingResponse() { ConnectionId = serviceId.Value.ToString() };
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

        // use the stored handle (strip leading @) so handle->PDS resolution runs the same as initial link.
        var identifier = existing.ServiceProfile.Username?.TrimStart('@')
            ?? existing.ServiceProfile.UserId;

        using var scope = serviceProvider.CreateScope();
        var handler = await ResolveOAuthHandlerAsync(serviceDescription, scope.ServiceProvider);
        var data = await handler.BeginAccountLinkAsync(userId, identifier);
        data.ExistingConnectionId = connectionId;

        await pendingOAuths.SetAsync(data);

        return new BeginAccountLinkingResponse() { RedirectUri = data.RedirectUri };
    }

    [Authorize]
    public override async Task<FinaliseAccountLinkingResponse> LinkServiceWithCredentials(CredentialLinkRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!connectedServices.TryGetValue(request.Service, out var serviceDescription))
            throw new RpcException(new Status(StatusCode.Unavailable, "This service is unsupported at this time."));

        if (serviceDescription.LinkMode != ServiceLinkMode.Credentials || serviceDescription.CredentialHandler == null)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "This service does not accept credentials."));

        using var scope = serviceProvider.CreateScope();
        var handler = await serviceDescription.CredentialHandler(scope.ServiceProvider);

        var connection = request.HasConnectionId
            ? await LoadOwnedConnectionAsync(request.ConnectionId, userId)
            : new LiveConnectedService()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Service = default!,
                AccessToken = default!,
                RefreshToken = default!,
                ExpiresAt = default!,
                Flags = LiveConnectedServiceFlags.None,
                AvailableCapabilities = serviceDescription.ServiceCapabilities,
                EnabledCapabilities = 0
            };

        try
        {
            connection = await handler.LinkAsync(
                connection,
                new CredentialLink(request.ServiceUrl, request.Username, request.Secret, request.HasLabel ? request.Label : null),
                context.CancellationToken);
        }
        catch (CredentialLinkException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }

        if (request.HasConnectionId)
            dbContext.ConnectedServices.Update(connection);
        else
            await dbContext.ConnectedServices.AddAsync(connection);

        await dbContext.SaveChangesAsync();

        return new FinaliseAccountLinkingResponse() { ConnectionId = connection.Id.ToString() };
    }

    private async Task<LiveConnectedService> LoadOwnedConnectionAsync(string connectionId, Guid userId)
    {
        if (!Guid.TryParse(connectionId, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid connection ID."));

        var existing = await dbContext.ConnectedServices.FindAsync(id)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Connection no longer exists."));

        if (existing.UserId != userId)
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Connection does not belong to this user."));

        return existing;
    }

    private static async Task<IOAuthProvider> ResolveOAuthHandlerAsync(ConnectedServiceDescription description, IServiceProvider services)
    {
        if (description.OAuthHandler == null)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "This service is not linked with OAuth."));

        return await description.OAuthHandler(services);
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
                Capabilities = (uint)connection.ServiceCapabilities
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
            var connection = new Connection()
            {
                Id = item.Id.ToString(),
                Service = item.Service,
                ServiceUrl = item.ServiceUrl,
                Capabilities = (uint)item.EnabledCapabilities,
                Flags = (ulong)item.Flags,
                UserId = item.ServiceProfile.UserId,
                UserName = item.ServiceProfile.Username,
            };

            if (item.ServiceProfile.Label != null)
                connection.Label = item.ServiceProfile.Label;

            await responseStream.WriteAsync(connection);
        }
    }

    public override async Task<DeleteConnectionResponse> DeleteConnection(DeleteConnectionRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        var connId = Guid.Parse(request.ConnectionId);

        var connection = await dbContext.ConnectedServices.FirstOrDefaultAsync(r => r.UserId == userId && r.Id == connId)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Connection not found!"));

        dbContext.ConnectedServices.Remove(connection);
        await dbContext.SaveChangesAsync();

        return new DeleteConnectionResponse();
    }

    public override async Task<Empty> UpdateCapabilities(UpdateCapabilitiesRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        var connId = Guid.Parse(request.ConnectionId);

        var connection = await dbContext.ConnectedServices.AsTracking().FirstOrDefaultAsync(r => r.UserId == userId && r.Id == connId)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Connection not found!"));

        // Heal rows created before the AvailableCapabilities column existed (migration default was 0)
        if (connection.AvailableCapabilities == LiveConnectedServiceCapabilities.None &&
            connectedServices.TryGetValue(connection.Service, out var serviceDescription))
        {
            connection.AvailableCapabilities = serviceDescription.ServiceCapabilities;
        }

        connection.EnabledCapabilities = (LiveConnectedServiceCapabilities)request.Capabilities
            & connection.AvailableCapabilities
            & LiveConnectedServiceCapabilities.All;

        // TODO: some capabilities can only support one enabled service, so we need to verfiy that
        // TODO: move this somewhere that isn't here, it should be validated Everywhere
        LiveConnectedServiceCapabilities[] singleCaps = [
            LiveConnectedServiceCapabilities.PhotoSync,
            LiveConnectedServiceCapabilities.Xbox,
            LiveConnectedServiceCapabilities.Zune
        ];

        await foreach (var otherConnection in dbContext.ConnectedServices
            .AsTracking()
            .Where(c => c.UserId == userId && c.Id != connId)
            .AsAsyncEnumerable())
        {
            foreach (var cap in singleCaps)
            {
                if ((connection.EnabledCapabilities & cap) == 0)
                    continue;

                // TODO: is it worth reporting these back?
                if ((otherConnection.EnabledCapabilities & cap) == cap)
                    otherConnection.EnabledCapabilities &= ~cap;
            }
        }

        await dbContext.SaveChangesAsync();

        return new Empty();
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
