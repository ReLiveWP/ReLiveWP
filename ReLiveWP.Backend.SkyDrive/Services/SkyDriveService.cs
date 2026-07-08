using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.SkyDrive.Data;
using ReLiveWP.Identity;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.SkyDrive.Services;

[Authorize]
public class SkyDriveService(SkyDriveDbContext dbContext,
                             ConnectedServices.ConnectedServicesClient connectedServicesClient,
                             IEnumerable<IPhotoSyncProxyClient> photoSyncProxyClients,
                             ILogger<SkyDriveService> logger) : global::ReLiveWP.Services.Grpc.SkyDrive.SkyDriveBase
{
    private const uint PhotoSyncCapability = 0x10;
    public override async Task<LibraryReply> GetLibrary(GetLibraryRequest request, ServerCallContext context)
    {
        var ownerId = GetOwnerId(context);
        var library = await dbContext.Libraries
            .FirstOrDefaultAsync(l => l.OwnerId == ownerId && l.Category == request.Category);

        if (library == null)
            return new LibraryReply { Exists = false };

        return new LibraryReply { Exists = true, Library = ToProto(library) };
    }

    public override async Task<ListLibrariesReply> ListLibraries(ListLibrariesRequest request, ServerCallContext context)
    {
        var ownerId = GetOwnerId(context);

        // Bootstrap the defaults so a never-provisioned user still gets a populated feed, mirroring
        // the lazy EnsureLibraryAsync in UploadPhoto/ProvisionUser.
        foreach (var category in DefaultLibraries)
            await EnsureLibraryAsync(ownerId, category);

        var libraries = await dbContext.Libraries
            .Where(l => l.OwnerId == ownerId)
            .ToListAsync();

        var reply = new ListLibrariesReply();
        reply.Libraries.AddRange(libraries.Select(ToProto));
        return reply;
    }

    public override async Task<LibraryReply> CreateOrUpdateLibrary(CreateOrUpdateLibraryRequest request, ServerCallContext context)
    {
        var ownerId = GetOwnerId(context);
        var now = DateTimeOffset.UtcNow;

        var library = await dbContext.Libraries.AsTracking()
            .FirstOrDefaultAsync(l => l.OwnerId == ownerId && l.Category == request.Category);

        if (library == null)
        {
            library = new SkyLibrary
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Category = request.Category,
                Created = now,
            };
            dbContext.Libraries.Add(library);
        }

        library.Type = request.Type;
        library.SharingLevel = request.SharingLevel;
        library.Title = request.Title;
        library.Summary = request.Summary;
        library.EmailKeyword = request.EmailKeyword;
        library.Updated = now;

        await dbContext.SaveChangesAsync();

        return new LibraryReply { Exists = true, Library = ToProto(library) };
    }

    private static readonly string[] DefaultLibraries = ["wmphotos", "mobilephotos"];

    public override async Task<Empty> ProvisionUser(ProvisionUserRequest request, ServerCallContext context)
    {
        var ownerId = GetOwnerId(context);

        foreach (var category in DefaultLibraries)
            await EnsureLibraryAsync(ownerId, category);

        return new Empty();
    }

    private async Task EnsureLibraryAsync(Guid ownerId, string category)
    {
        if (await dbContext.Libraries.AnyAsync(l => l.OwnerId == ownerId && l.Category == category))
            return;

        var now = DateTimeOffset.UtcNow;
        dbContext.Libraries.Add(new SkyLibrary
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Category = category,
            Type = "Library",
            SharingLevel = "private",
            Created = now,
            Updated = now,
        });

        await dbContext.SaveChangesAsync();
    }

    public override async Task<PhotoReply> UploadPhoto(IAsyncStreamReader<UploadPhotoRequest> requestStream, ServerCallContext context)
    {
        var ct = context.CancellationToken;

        PhotoUploadMetadata? metadata = null;
        using var buffer = new MemoryStream();

        await foreach (var message in requestStream.ReadAllAsync(ct))
        {
            switch (message.PayloadCase)
            {
                case UploadPhotoRequest.PayloadOneofCase.Metadata:
                    metadata = message.Metadata;
                    break;
                case UploadPhotoRequest.PayloadOneofCase.Chunk:
                    buffer.Write(message.Chunk.Span);
                    break;
            }
        }

        if (metadata == null)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Missing photo metadata."));

        var userId = GetUserId(context);
        var ownerId = Guid.Parse(userId);
        var photo = new PhotoUpload(metadata.FileName, metadata.ContentType, metadata.Summary, buffer.ToArray());
        var reply = new PhotoReply();

        await EnsureLibraryAsync(ownerId, metadata.Category);

        var call = connectedServicesClient.GetConnections(new ConnectionsRequest { Capabilities = PhotoSyncCapability }, cancellationToken: ct);

        await foreach (var connection in call.ResponseStream.ReadAllAsync(ct))
        {
            var proxyClient = photoSyncProxyClients.FirstOrDefault(p => p.ServiceId == connection.Service);
            if (proxyClient == null)
            {
                logger.LogWarning("No photo-sync proxy client registered for {Service}", connection.Service);
                continue;
            }

            try
            {
                var result = await proxyClient.UploadAsync(userId, connection.Id, photo, ct);
                reply.ProviderItems.Add(new ProviderItem { Service = connection.Service, ItemId = result.ItemId, Url = result.Url ?? "" });

                logger.LogInformation("Synced photo {FileName} to {Service} as {ItemId}", photo.FileName, connection.Service, result.ItemId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync photo to {Service} ({ConnectionId})", connection.Service, connection.Id);
            }
        }

        var skyPhoto = new SkyPhoto
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Category = metadata.Category,
            FileName = metadata.FileName,
            ContentType = string.IsNullOrEmpty(metadata.ContentType) ? null : metadata.ContentType,
            Summary = string.IsNullOrEmpty(metadata.Summary) ? null : metadata.Summary,
            ProviderItemsJson = JsonSerializer.Serialize(
                reply.ProviderItems.Select(p => new { p.Service, ItemId = p.ItemId, p.Url })),
            Created = DateTimeOffset.UtcNow,
        };

        dbContext.Photos.Add(skyPhoto);
        await dbContext.SaveChangesAsync(ct);

        reply.Id = skyPhoto.Id.ToString();
        return reply;
    }

    public override async Task<Empty> SetAlbumPermissions(SetAlbumPermissionsRequest request, ServerCallContext context)
    {
        var ownerId = GetOwnerId(context);
        var library = await dbContext.Libraries.AsTracking()
            .FirstOrDefaultAsync(l => l.OwnerId == ownerId && l.Category == request.Category)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Library not found!"));

        library.SharingLevel = request.SharingLevel;
        library.Updated = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        return new Empty();
    }

    // Owner is always the authenticated caller; the UserId carried in requests is ignored so a
    // caller can't read or mutate another user's libraries by spoofing it.
    private static string GetUserId(ServerCallContext context)
        => context.GetHttpContext().User.Id()
           ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid user."));

    private static Guid GetOwnerId(ServerCallContext context)
        => Guid.Parse(GetUserId(context));

    private static Library ToProto(SkyLibrary library) => new()
    {
        Id = library.Id.ToString(),
        Type = library.Type ?? "",
        Category = library.Category,
        SharingLevel = library.SharingLevel ?? "",
        Title = library.Title ?? "",
        Summary = library.Summary ?? "",
        EmailKeyword = library.EmailKeyword ?? "",
    };
}
