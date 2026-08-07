using System.Runtime.CompilerServices;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using ReLiveWP.Identity;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.SkyDocs;

namespace ReLiveWP.Backend.SkyDrive.Services;

[Authorize]
public class SkyDocsService(ConnectedServices.ConnectedServicesClient connectedServicesClient,
                            IEnumerable<IFileSyncProxyClient> fileSyncProxyClients,
                            SyncCursorStore syncCursors,
                            ILogger<SkyDocsService> logger)
    : SkyDocs.SkyDocsBase
{
    private const uint FileStorageCapability = 0x800;

    public override async Task<ListDocLibrariesReply> ListLibraries(ListDocLibrariesRequest request, ServerCallContext context)
    {
        var ct = context.CancellationToken;
        var userId = GetUserId(context);
        var reply = new ListDocLibrariesReply();

        await foreach (var (client, connectionId) in GetProvidersAsync(ct))
        {
            try
            {
                foreach (var library in await client.ListLibrariesAsync(userId, connectionId, ct))
                    reply.Libraries.Add(ToProto(library));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to list document libraries from {Service}", client.ServiceId);
            }
        }

        return reply;
    }

    public override async Task<GetDocItemReply> GetItem(GetDocItemRequest request, ServerCallContext context)
    {
        var ct = context.CancellationToken;
        var userId = GetUserId(context);

        await foreach (var (client, connectionId) in GetProvidersAsync(ct))
        {
            try
            {
                var item = await client.GetItemAsync(userId, connectionId, request.Path, ct);
                if (item != null)
                    return new GetDocItemReply { Exists = true, Item = ToProto(item) };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to stat {Path} from {Service}", request.Path, client.ServiceId);
            }
        }

        return new GetDocItemReply { Exists = false };
    }

    public override async Task<ListDocItemsReply> ListItems(ListDocItemsRequest request, ServerCallContext context)
    {
        var ct = context.CancellationToken;
        var userId = GetUserId(context);
        var reply = new ListDocItemsReply();

        await foreach (var (client, connectionId) in GetProvidersAsync(ct))
        {
            try
            {
                foreach (var entry in await client.ListChildrenAsync(userId, connectionId, request.Path, request.Recursive, ct))
                    reply.Items.Add(ToProto(entry));

                reply.Exists = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to list {Path} from {Service}", request.Path, client.ServiceId);
            }
        }

        return reply;
    }

    public override async Task<GetDocChangesReply> GetChanges(GetDocChangesRequest request, ServerCallContext context)
    {
        var ct = context.CancellationToken;
        var userId = GetUserId(context);
        var reply = new GetDocChangesReply();

        var presented = !string.IsNullOrEmpty(request.SyncToken);

        await foreach (var (client, connectionId) in GetProvidersAsync(ct))
        {
            var cursor = await syncCursors.ResolveAsync(request.SyncToken, client.ServiceId);

            if (presented && cursor == null)
            {
                logger.LogInformation("Rejecting unknown sync token for {Path}, telling the client to restart", request.Path);
                return new GetDocChangesReply();
            }

            try
            {
                if (!client.SupportsDelta)
                {
                    foreach (var entry in await client.ListChildrenAsync(userId, connectionId, request.Path, true, ct))
                        reply.Changed.Add(ToProto(entry));

                    continue;
                }

                var changes = await client.GetChangesAsync(userId, connectionId, request.Path, cursor, ct);

                foreach (var entry in changes.Changed)
                    reply.Changed.Add(ToProto(entry));

                reply.DeletedPaths.AddRange(changes.DeletedPaths);

                if (!string.IsNullOrEmpty(changes.Cursor))
                    reply.SyncToken = await syncCursors.IssueAsync(client.ServiceId, changes.Cursor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get changes for {Path} from {Service}", request.Path, client.ServiceId);
            }
        }

        return reply;
    }

    public override async Task<GetDocContentLocationReply> GetContentLocation(GetDocContentLocationRequest request, ServerCallContext context)
    {
        var ct = context.CancellationToken;
        var userId = GetUserId(context);

        await foreach (var (client, connectionId) in GetProvidersAsync(ct))
        {
            try
            {
                var location = await client.GetContentLocationAsync(userId, connectionId, request.Path, ct);
                if (location == null)
                    continue;

                var reply = new GetDocContentLocationReply
                {
                    Exists = true,
                    Url = location.Url,
                    ContentType = location.ContentType,
                    Size = location.Size,
                    Etag = location.ETag ?? "",
                };

                foreach (var (name, value) in location.Headers)
                    reply.Headers[name] = value;

                return reply;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to resolve {Path} from {Service}", request.Path, client.ServiceId);
            }
        }

        return new GetDocContentLocationReply { Exists = false };
    }

    private async IAsyncEnumerable<(IFileSyncProxyClient Client, string ConnectionId)> GetProvidersAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        var call = connectedServicesClient.GetConnections(
            new ConnectionsRequest { Capabilities = FileStorageCapability }, cancellationToken: ct);

        await foreach (var connection in call.ResponseStream.ReadAllAsync(ct))
        {
            var client = fileSyncProxyClients.FirstOrDefault(p => p.ServiceId == connection.Service);
            if (client != null)
                yield return (client, connection.Id);
        }
    }

    private static DocLibrary ToProto(ProviderLibrary library) => new()
    {
        Path = library.Path,
        DisplayName = library.DisplayName,
        ResourceId = library.ResourceId,
        AccessLevel = library.ReadOnly ? DocAccessLevel.Read : DocAccessLevel.ReadWrite,
        SharingLevel = DocSharingLevel.Private,
        ModifiedUnix = library.Modified.ToUnixTimeSeconds(),
        Owner = "",
    };

    private static DocItem ToProto(ProviderEntry entry) => new()
    {
        Name = entry.Name,
        Path = entry.Path,
        IsFolder = entry.IsFolder,
        Size = entry.Size,
        CreatedUnix = entry.Created.ToUnixTimeSeconds(),
        ModifiedUnix = entry.Modified.ToUnixTimeSeconds(),
        ContentType = entry.ContentType,
        Etag = entry.ETag ?? "",
        ReadOnly = entry.ReadOnly,
        ResourceId = entry.ResourceId,
        ProgId = entry.ProgId ?? "",
    };

    private static string GetUserId(ServerCallContext context)
        => context.GetHttpContext().User.Id()
           ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid user."));
}
