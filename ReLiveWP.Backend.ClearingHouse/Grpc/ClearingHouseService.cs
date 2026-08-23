using System.Security.Claims;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ClearingHouse.Data;
using ReLiveWP.Backend.ClearingHouse.Services.Mirror;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.ClearingHouse;
using ClearingHouseBase = ReLiveWP.Services.Grpc.ClearingHouse.ClearingHouse.ClearingHouseBase;

namespace ReLiveWP.Backend.ClearingHouse.Grpc;

public class ClearingHouseService(
    ConnectedServices.ConnectedServicesClient connectedServices,
    ClearingHouseDbContext db,
    MirrorDriverRegistry drivers,
    ILogger<ClearingHouseService> logger)
    : ClearingHouseBase
{
    public override async Task<SyncStatus> SyncNow(SyncNowRequest request, ServerCallContext context)
    {
        var kind = Kind(request.Kind);
        var connection = await ResolveConnectionAsync(kind, request.ConnectionId, context);
        var sources = await EnrolAsync(kind, connection, context.CancellationToken);

        foreach (var source in sources) Request(source);

        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("queued a {Service} {Kind} sync for {User}",
            connection.ServiceId, kind, connection.Connection.UserId);

        return Aggregate(kind, connection.ConnectionId, connection.ServiceId, sources);
    }

    public override async Task<SyncStatus> SetSync(SetSyncRequest request, ServerCallContext context)
    {
        var kind = Kind(request.Kind);

        if (!request.Enabled) return await DisableAsync(kind, request.ConnectionId, context);

        var connection = await ResolveConnectionAsync(kind, request.ConnectionId, context);
        var sources = await EnrolAsync(kind, connection, context.CancellationToken);

        foreach (var source in sources)
        {
            source.SyncEnabled = true;
            Request(source);
        }

        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("enabled {Service} {Kind} sync for {User}",
            connection.ServiceId, kind, connection.Connection.UserId);

        return Aggregate(kind, connection.ConnectionId, connection.ServiceId, sources);
    }

    private async Task<SyncStatus> DisableAsync(MirrorKind kind, string connectionId, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "connection_id is required"));

        var sources = await ExistingAsync(kind, GetUserId(context), connectionId, context.CancellationToken);

        foreach (var source in sources) source.SyncEnabled = false;

        await db.SaveChangesAsync(context.CancellationToken);

        return Aggregate(kind, connectionId, sources.FirstOrDefault()?.ServiceId ?? string.Empty, sources);
    }

    public override async Task<GetSyncResponse> GetSync(GetSyncRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var query = db.SyncSources.AsNoTracking().Where(s => s.UserId == userId);

        if (request.HasKind)
        {
            var kind = Kind(request.Kind);
            query = query.Where(s => s.Kind == kind);
        }

        if (request.HasConnectionId)
            query = query.Where(s => s.ConnectionId == request.ConnectionId);

        var response = new GetSyncResponse();

        var groups = (await query.ToListAsync(context.CancellationToken))
            .GroupBy(s => (s.Kind, s.ConnectionId));

        foreach (var group in groups)
            response.Connections.Add(
                Aggregate(group.Key.Kind, group.Key.ConnectionId, group.First().ServiceId, [.. group]));

        return response;
    }

    private Task<List<DbSyncSource>> ExistingAsync(
        MirrorKind kind, string userId, string connectionId, CancellationToken ct) =>
        db.SyncSources
            .Where(s => s.UserId == userId && s.ConnectionId == connectionId && s.Kind == kind)
            .ToListAsync(ct);

    private static void Request(DbSyncSource source)
    {
        source.RunRequestedAt = DateTime.UtcNow;
        source.ConsecutiveFailures = 0;
        source.LastFailure = null;
    }

    private async Task<List<DbSyncSource>> EnrolAsync(
        MirrorKind kind,
        (string ServiceId, string ConnectionId, SyncConnection Connection) connection,
        CancellationToken ct)
    {
        if (!drivers.TryGet(kind, connection.ServiceId, out var driver))
            throw new RpcException(new Status(
                StatusCode.Unimplemented, $"No {kind} driver for '{connection.ServiceId}'"));

        var userId = connection.Connection.UserId;
        var connectionId = connection.ConnectionId;

        var existing = await ExistingAsync(kind, userId, connectionId, ct);
        var discovered = await driver.ListSourcesAsync(connection.Connection, ct);
        var enabled = existing.Count > 0 && existing.All(s => s.SyncEnabled);

        foreach (var source in discovered)
        {
            if (existing.Any(s => s.SourceId == source.Id)) continue;

            var added = new DbSyncSource
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = userId,
                ConnectionId = connectionId,
                Kind = kind,
                ServiceId = connection.ServiceId,
                SourceId = source.Id,
                RemoteDisplayName = source.DisplayName,
                SyncEnabled = enabled,
            };

            db.SyncSources.Add(added);
            existing.Add(added);
        }

        return existing;
    }

    private static SyncStatus Aggregate(
        MirrorKind kind, string connectionId, string serviceId, IReadOnlyList<DbSyncSource> sources)
    {
        var status = new SyncStatus
        {
            Kind = Kind(kind),
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
        MirrorKind kind, string connectionId, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "connection_id is required"));

        var userId = GetUserId(context);

        var resolved = await ConnectionLookup.ResolveAsync(
                connectedServices, userId, connectionId, ct: context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Connection {connectionId} not found"));

        if (!resolved.Supplies(kind))
            throw new RpcException(new Status(
                StatusCode.FailedPrecondition, $"{kind} is turned off for connection {connectionId}"));

        return resolved.Usable is { } usable
            ? (resolved.ServiceId, connectionId, usable)
            : throw new RpcException(new Status(
                StatusCode.FailedPrecondition, $"Connection {connectionId} needs relinking"));
    }

    private static MirrorKind Kind(SyncKind kind) => kind switch
    {
        SyncKind.Contacts => MirrorKind.Contacts,
        SyncKind.Calendar => MirrorKind.Calendar,
        _ => throw new RpcException(new Status(StatusCode.InvalidArgument, $"Unknown sync kind {kind}")),
    };

    private static SyncKind Kind(MirrorKind kind) => kind switch
    {
        MirrorKind.Contacts => SyncKind.Contacts,
        MirrorKind.Calendar => SyncKind.Calendar,
        _ => throw new RpcException(new Status(StatusCode.Internal, $"Unknown mirror kind {kind}")),
    };

    private static string GetUserId(ServerCallContext context) =>
        context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid user."));
}
