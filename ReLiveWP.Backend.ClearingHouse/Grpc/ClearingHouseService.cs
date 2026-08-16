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
    public override async Task<ListContactSourcesResponse> ListContactSources(
        ListContactSourcesRequest request, ServerCallContext context)
    {
        var connection = await ResolveConnectionAsync(request.ConnectionId, context);

        if (!drivers.TryGet(connection.ServiceId, out var driver))
            throw new RpcException(new Status(StatusCode.Unimplemented, $"No contact driver for '{connection.ServiceId}'"));

        var sources = await driver.ListSourcesAsync(connection.Connection, context.CancellationToken);

        var response = new ListContactSourcesResponse { ServiceId = connection.ServiceId };
        foreach (var s in sources)
        {
            var source = new ContactSource { Id = s.Id, DisplayName = s.DisplayName, IsDefault = s.IsDefault };
            if (s.Count is { } count) source.Count = count;
            response.Sources.Add(source);
        }

        return response;
    }

    public override async Task<ImportContactsResult> ImportContacts(
        ImportContactsRequest request, ServerCallContext context)
    {
        var connection = await ResolveConnectionAsync(request.ConnectionId, context);

        if (!drivers.TryGet(connection.ServiceId, out var driver))
            throw new RpcException(new Status(StatusCode.Unimplemented, $"No contact driver for '{connection.ServiceId}'"));

        var detachAfterRun = !request.KeepInSync;
        var result = new ImportContactsResult();

        var discovered = await driver.ListSourcesAsync(connection.Connection, context.CancellationToken);
        var wanted = request.SourceIds.Count > 0
            ? request.SourceIds.Select(id =>
                discovered.FirstOrDefault(s => s.Id == id) ?? new RemoteContactSource(id, id)).ToList()
            : [.. discovered];

        foreach (var source in wanted)
        {
            await EnrolAsync(connection, source, detachAfterRun, context.CancellationToken);
            result.QueuedSourceIds.Add(source.Id);
        }

        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("queued {Count} {Service} source(s) for {User} to {What}",
            wanted.Count, connection.ServiceId, connection.Connection.UserId,
            detachAfterRun ? "import once" : "keep in sync");

        return result;
    }

    public override async Task<GetSyncStatusResponse> GetSyncStatus(GetSyncStatusRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var query = db.ContactSyncSources.AsNoTracking().Where(s => s.UserId == userId);
        if (request.HasConnectionId)
            query = query.Where(s => s.ConnectionId == request.ConnectionId);

        var response = new GetSyncStatusResponse();

        foreach (var s in await query.ToListAsync(context.CancellationToken))
        {
            var status = new SourceSyncStatus
            {
                ConnectionId = s.ConnectionId,
                ServiceId = s.ServiceId,
                SourceId = s.SourceId,
                DetachAfterRun = s.DetachAfterRun,
                Running = s.RunStartedAt is not null,
                Queued = s.RunRequestedAt is not null,
                ConsecutiveFailures = s.ConsecutiveFailures,
                Created = s.LastRunCreated,
                Updated = s.LastRunUpdated,
                Deleted = s.LastRunDeleted,
                Skipped = s.LastRunSkipped,
            };

            if (s.LastSyncedAt is { } synced) status.LastSyncedAt = synced.ToString("O");
            if (s.LastFailure is { } failure) status.LastFailure = failure;

            response.Sources.Add(status);
        }

        return response;
    }

    private async Task EnrolAsync(
        (string ServiceId, SyncConnection Connection) connection, RemoteContactSource source,
        bool detachAfterRun, CancellationToken ct)
    {
        var userId = connection.Connection.UserId;
        var connectionId = connection.Connection.ConnectionId;

        var existing = await db.ContactSyncSources.FirstOrDefaultAsync(
            s => s.UserId == userId && s.ConnectionId == connectionId && s.SourceId == source.Id, ct);

        if (existing is null)
        {
            db.ContactSyncSources.Add(new DbContactSyncSource
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = userId,
                ConnectionId = connectionId,
                ServiceId = connection.ServiceId,
                SourceId = source.Id,
                DetachAfterRun = detachAfterRun,
                RunRequestedAt = DateTime.UtcNow,
            });
            return;
        }

        existing.DetachAfterRun = detachAfterRun;
        existing.RunRequestedAt = DateTime.UtcNow;
        existing.ConsecutiveFailures = 0;
        existing.LastFailure = null;

        existing.DeltaToken = null;
    }

    private async Task<(string ServiceId, SyncConnection Connection)> ResolveConnectionAsync(
        string connectionId, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "connection_id is required"));

        var userId = GetUserId(context);

        var resolved = await ConnectionLookup.ResolveAsync(
                connectedServices, userId, connectionId, ct: context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Connection {connectionId} not found"));

        return resolved.Usable is { } usable
            ? (resolved.ServiceId, usable)
            : throw new RpcException(new Status(
                StatusCode.FailedPrecondition, $"Connection {connectionId} needs relinking"));
    }

    private static string GetUserId(ServerCallContext context) =>
        context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid user."));
}
