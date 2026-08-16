using System.Security.Claims;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ClearingHouse.Data;
using ReLiveWP.Backend.ClearingHouse.Services.ContactSync;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.ClearingHouse;
using ClearingHouseBase = ReLiveWP.Services.Grpc.ClearingHouse.ClearingHouse.ClearingHouseBase;

namespace ReLiveWP.Backend.ClearingHouse.Grpc;

public class ClearingHouseService(
    ConnectedServices.ConnectedServicesClient connectedServices,
    ClearingHouseDbContext db,
    ContactSyncDriverRegistry drivers,
    ILogger<ClearingHouseService> logger)
    : ClearingHouseBase
{
    public override async Task<ContactSyncStatus> SyncContactsNow(
        ContactSyncRequest request, ServerCallContext context)
    {
        var connection = await ResolveConnectionAsync(request.ConnectionId, context);
        var sources = await EnrolAsync(connection, context.CancellationToken);

        foreach (var source in sources) Request(source);

        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("queued a {Service} contact sync for {User}",
            connection.ServiceId, connection.Connection.UserId);

        return Aggregate(connection.ConnectionId, connection.ServiceId, sources);
    }

    public override async Task<ContactSyncStatus> SetContactSync(
        SetContactSyncRequest request, ServerCallContext context)
    {
        if (!request.Enabled) return await DisableAsync(request.ConnectionId, context);

        var connection = await ResolveConnectionAsync(request.ConnectionId, context);
        var sources = await EnrolAsync(connection, context.CancellationToken);

        foreach (var source in sources)
        {
            source.SyncEnabled = true;
            Request(source);
        }

        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("enabled {Service} contact sync for {User}",
            connection.ServiceId, connection.Connection.UserId);

        return Aggregate(connection.ConnectionId, connection.ServiceId, sources);
    }

    private async Task<ContactSyncStatus> DisableAsync(string connectionId, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "connection_id is required"));

        var sources = await ExistingAsync(GetUserId(context), connectionId, context.CancellationToken);

        foreach (var source in sources) source.SyncEnabled = false;

        await db.SaveChangesAsync(context.CancellationToken);

        return Aggregate(connectionId, sources.FirstOrDefault()?.ServiceId ?? string.Empty, sources);
    }

    public override async Task<GetContactSyncResponse> GetContactSync(
        GetContactSyncRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var query = db.ContactSyncSources.AsNoTracking().Where(s => s.UserId == userId);
        if (request.HasConnectionId)
            query = query.Where(s => s.ConnectionId == request.ConnectionId);

        var response = new GetContactSyncResponse();

        foreach (var group in (await query.ToListAsync(context.CancellationToken)).GroupBy(s => s.ConnectionId))
            response.Connections.Add(Aggregate(group.Key, group.First().ServiceId, [.. group]));

        return response;
    }

    private Task<List<DbContactSyncSource>> ExistingAsync(string userId, string connectionId, CancellationToken ct) =>
        db.ContactSyncSources
            .Where(s => s.UserId == userId && s.ConnectionId == connectionId)
            .ToListAsync(ct);

    private static void Request(DbContactSyncSource source)
    {
        source.RunRequestedAt = DateTime.UtcNow;
        source.ConsecutiveFailures = 0;
        source.LastFailure = null;
    }

    private async Task<List<DbContactSyncSource>> EnrolAsync(
        (string ServiceId, string ConnectionId, SyncConnection Connection) connection, CancellationToken ct)
    {
        if (!drivers.TryGet(connection.ServiceId, out var driver))
            throw new RpcException(new Status(
                StatusCode.Unimplemented, $"No contact driver for '{connection.ServiceId}'"));

        var userId = connection.Connection.UserId;
        var connectionId = connection.ConnectionId;

        var existing = await ExistingAsync(userId, connectionId, ct);
        var discovered = await driver.ListSourcesAsync(connection.Connection, ct);
        var enabled = existing.Count > 0 && existing.All(s => s.SyncEnabled);

        foreach (var source in discovered)
        {
            if (existing.Any(s => s.SourceId == source.Id)) continue;

            var added = new DbContactSyncSource
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = userId,
                ConnectionId = connectionId,
                ServiceId = connection.ServiceId,
                SourceId = source.Id,
                SyncEnabled = enabled,
            };

            db.ContactSyncSources.Add(added);
            existing.Add(added);
        }

        return existing;
    }

    private static ContactSyncStatus Aggregate(
        string connectionId, string serviceId, IReadOnlyList<DbContactSyncSource> sources)
    {
        var status = new ContactSyncStatus
        {
            ConnectionId = connectionId,
            ServiceId = serviceId,
            Enabled = sources.Count > 0 && sources.All(s => s.SyncEnabled),
            Running = sources.Any(s => s.RunStartedAt is not null),
            Queued = sources.Any(s => s.RunRequestedAt is not null),
            Created = sources.Sum(s => s.LastRunCreated),
            Updated = sources.Sum(s => s.LastRunUpdated),
            Deleted = sources.Sum(s => s.LastRunDeleted),
            Skipped = sources.Sum(s => s.LastRunSkipped),
        };

        if (sources.Count > 0 && sources.All(s => s.LastSyncedAt is not null))
            status.LastSyncedAt = sources.Min(s => s.LastSyncedAt)!.Value.ToString("O");

        if (sources.FirstOrDefault(s => s.LastFailure is not null)?.LastFailure is { } failure)
            status.LastFailure = failure;

        return status;
    }

    private async Task<(string ServiceId, string ConnectionId, SyncConnection Connection)> ResolveConnectionAsync(
        string connectionId, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "connection_id is required"));

        var userId = GetUserId(context);

        var resolved = await ConnectionLookup.ResolveAsync(
                connectedServices, userId, connectionId, ct: context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Connection {connectionId} not found"));

        if (!resolved.SuppliesContacts)
            throw new RpcException(new Status(
                StatusCode.FailedPrecondition, $"Contacts are turned off for connection {connectionId}"));

        return resolved.Usable is { } usable
            ? (resolved.ServiceId, connectionId, usable)
            : throw new RpcException(new Status(
                StatusCode.FailedPrecondition, $"Connection {connectionId} needs relinking"));
    }

    private static string GetUserId(ServerCallContext context) =>
        context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid user."));
}
