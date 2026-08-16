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
                             ILogger<SkyDriveService> logger) : ReLiveWP.Services.Grpc.SkyDrive.SkyDriveBase
{
    private const uint PhotoSyncCapability = 0x10;
    private const long MaxUploadBytes = 100L * 1024 * 1024;
    private const string VideoMediaType = "video";

    public override async Task<LibraryReply> GetLibrary(GetLibraryRequest request, ServerCallContext context)
    {
        var ownerId = GetOwnerId(context);

        if (DefaultLibraries.Contains(request.Category))
            await EnsureLibraryAsync(ownerId, request.Category);

        var library = await dbContext.Libraries
            .FirstOrDefaultAsync(l => l.OwnerId == ownerId && l.Category == request.Category);

        if (library == null)
            return new LibraryReply { Exists = false };

        var coverRef = await CoverRefAsync(ownerId, library.Id.ToString(), context.CancellationToken);
        return new LibraryReply { Exists = true, Library = ToProto(library, coverRef) };
    }

    public override async Task<ListLibrariesReply> ListLibraries(ListLibrariesRequest request, ServerCallContext context)
    {
        var ownerId = GetOwnerId(context);
        var ct = context.CancellationToken;

        foreach (var category in DefaultLibraries)
            await EnsureLibraryAsync(ownerId, category);

        var libraries = await dbContext.Libraries
            .Where(l => l.OwnerId == ownerId)
            .ToListAsync(ct);

        var covers = await CoverRefsAsync(ownerId, libraries.Select(l => l.Id.ToString()).ToList(), ct);

        var reply = new ListLibrariesReply();
        reply.Libraries.AddRange(libraries.Select(l => ToProto(l, Cover(covers, l.Id.ToString()))));
        return reply;
    }

    public override async Task<GetAlbumCoversReply> GetAlbumCovers(GetAlbumCoversRequest request, ServerCallContext context)
    {
        var ownerId = GetOwnerId(context);
        var covers = await CoverRefsAsync(ownerId, request.AlbumRefs, context.CancellationToken);

        var reply = new GetAlbumCoversReply();
        foreach (var (albumRef, resourceRef) in covers)
            reply.Covers.Add(new AlbumCover { AlbumRef = albumRef, ResourceRef = resourceRef });

        return reply;
    }

    public override async Task<Empty> SetAlbumCover(SetAlbumCoverRequest request, ServerCallContext context)
    {
        var ownerId = GetOwnerId(context);

        if (string.IsNullOrEmpty(request.AlbumRef))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Missing album reference."));

        await SetCoverAsync(ownerId, request.AlbumRef, request.ResourceRef, context.CancellationToken);
        return new Empty();
    }

    private static string Cover(Dictionary<string, string> covers, string albumRef)
        => covers.TryGetValue(albumRef, out var found) ? found : "";

    private Task<Dictionary<string, string>> CoverRefsAsync(Guid ownerId, IReadOnlyCollection<string> albumRefs, CancellationToken ct)
        => albumRefs.Count == 0
            ? Task.FromResult(new Dictionary<string, string>())
            : dbContext.AlbumCovers
                .Where(c => c.OwnerId == ownerId && albumRefs.Contains(c.AlbumRef))
                .ToDictionaryAsync(c => c.AlbumRef, c => c.ResourceRef, ct);

    private async Task<string> CoverRefAsync(Guid ownerId, string albumRef, CancellationToken ct)
        => await dbContext.AlbumCovers
            .Where(c => c.OwnerId == ownerId && c.AlbumRef == albumRef)
            .Select(c => c.ResourceRef)
            .FirstOrDefaultAsync(ct) ?? "";

    private async Task SetCoverAsync(Guid ownerId, string albumRef, string resourceRef, CancellationToken ct)
    {
        var existing = await dbContext.AlbumCovers.AsTracking()
            .FirstOrDefaultAsync(c => c.OwnerId == ownerId && c.AlbumRef == albumRef, ct);

        if (string.IsNullOrEmpty(resourceRef))
        {
            if (existing == null)
                return;

            dbContext.AlbumCovers.Remove(existing);
        }
        else if (existing == null)
        {
            dbContext.AlbumCovers.Add(new SkyAlbumCover
            {
                OwnerId = ownerId,
                AlbumRef = albumRef,
                ResourceRef = resourceRef,
                Updated = DateTimeOffset.UtcNow,
            });
        }
        else if (existing.ResourceRef == resourceRef)
        {
            return;
        }
        else
        {
            existing.ResourceRef = resourceRef;
            existing.Updated = DateTimeOffset.UtcNow;
        }

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // a concurrent listing of the same album got there first, and it wrote the same photo
            dbContext.ChangeTracker.Clear();
            logger.LogDebug(ex, "Cover for {AlbumRef} lost a race, leaving it to the next list", albumRef);
        }
    }

    public override async Task<ListPhotosReply> ListPhotos(ListPhotosRequest request, ServerCallContext context)
    {
        var ownerId = GetOwnerId(context);
        SkyLibrary? library;

        if (request.AlbumCase == ListPhotosRequest.AlbumOneofCase.Category)
        {
            if (DefaultLibraries.Contains(request.Category))
                await EnsureLibraryAsync(ownerId, request.Category);

            library = await dbContext.Libraries
                .FirstOrDefaultAsync(l => l.OwnerId == ownerId && l.Category == request.Category);
        }
        else if (Guid.TryParse(request.LibraryId, out var libraryId))
        {
            library = await dbContext.Libraries
                .FirstOrDefaultAsync(l => l.OwnerId == ownerId && l.Id == libraryId);
        }
        else
        {
            library = null;
        }

        if (library == null)
            return new ListPhotosReply { Exists = false };

        var ct = context.CancellationToken;
        var userId = GetUserId(context);
        var albumRef = library.Id.ToString();
        var reply = new ListPhotosReply { Exists = true, Library = ToProto(library) };

        var membership = (await dbContext.AlbumItems
            .Where(i => i.OwnerId == ownerId && i.Album == library.Category)
            .Select(i => i.Provider + "+" + i.ProviderItemId)
            .ToListAsync(ct))
            .ToHashSet();

        var items = new List<(ProviderPhoto Photo, string Provider)>();
        await foreach (var (client, connectionId) in GetReadableProvidersAsync(ct))
        {
            var albumId = await dbContext.ProviderAlbums
                .Where(a => a.OwnerId == ownerId && a.Album == library.Category && a.Provider == client.ServiceId)
                .Select(a => a.ProviderAlbumId)
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrEmpty(albumId))
                continue;

            try
            {
                foreach (var photo in await client.ListAsync(userId, connectionId, albumId, ct))
                    items.Add((photo, client.ServiceId));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to list photos from {Service}", client.ServiceId);
            }
        }

        // an empty list is as likely to be a provider that just failed on us as an empty album, and
        // blanking the tile over that is worse than showing a slightly old cover
        if (items.Count == 0)
        {
            reply.Library.CoverRef = await CoverRefAsync(ownerId, albumRef, ct);
            return reply;
        }

        await AdoptAlbumItemsAsync(ownerId, library.Category, items, membership, ct);

        foreach (var (photo, provider) in items.OrderByDescending(i => i.Photo.Created))
            reply.Photos.Add(ToPhotoItem(photo, provider));

        var cover = reply.Photos.FirstOrDefault(p => p.MediaType != VideoMediaType);
        if (cover != null)
            await SetCoverAsync(ownerId, albumRef, cover.ResourceRef, ct);

        reply.Library.CoverRef = cover?.ResourceRef ?? await CoverRefAsync(ownerId, albumRef, ct);
        return reply;
    }

    // a provider album is the source of truth for its own contents, so anything we find in one that we
    // don't already track gets a row, keeping the GetPhotoContent ownership check honest for photos the
    // phone never uploaded itself.
    private async Task AdoptAlbumItemsAsync(Guid ownerId, string album,
                                            List<(ProviderPhoto Photo, string Provider)> items,
                                            HashSet<string> membership, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var unknown = items
            .Where(i => !membership.Contains($"{i.Provider}+{i.Photo.ItemId}"))
            .DistinctBy(i => $"{i.Provider}+{i.Photo.ItemId}")
            .Select(i => new SkyAlbumItem
            {
                OwnerId = ownerId,
                Album = album,
                Provider = i.Provider,
                ProviderItemId = i.Photo.ItemId,
                SuppressNotification = true,
                Created = now,
            })
            .ToList();

        if (unknown.Count == 0)
            return;

        dbContext.AlbumItems.AddRange(unknown);

        try
        {
            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("Adopted {Count} untracked item(s) into {Album} for {OwnerId}", unknown.Count, album, ownerId);
        }
        catch (DbUpdateException ex)
        {
            // a concurrent list adopted these first; anything it didn't cover comes back on the next call
            foreach (var item in unknown)
                dbContext.Entry(item).State = EntityState.Detached;

            logger.LogDebug(ex, "Adoption for {Album} lost a race, leaving it to the next list", album);
        }
    }

    private static PhotoItem ToPhotoItem(ProviderPhoto photo, string provider) => new()
    {
        ResourceRef = $"{provider}+{photo.ItemId}",
        FileName = photo.FileName,
        ContentType = photo.ContentType,
        Summary = photo.Description ?? "",
        MediaType = photo.IsVideo ? VideoMediaType : "photo",
        CreatedUnix = photo.Created.ToUnixTimeSeconds(),
        Width = photo.Width,
        Height = photo.Height,
    };

    private async IAsyncEnumerable<(IPhotoSyncProxyClient Client, string ConnectionId)> GetReadableProvidersAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var call = connectedServicesClient.GetConnections(
            new ConnectionsRequest { Capabilities = PhotoSyncCapability }, cancellationToken: ct);

        await foreach (var connection in call.ResponseStream.ReadAllAsync(ct))
        {
            var client = photoSyncProxyClients.FirstOrDefault(p => p.ServiceId == connection.Service);
            if (client != null)
                yield return (client, connection.Id);
        }
    }

    public override async Task<GetPhotoContentReply> GetPhotoContent(GetPhotoContentRequest request, ServerCallContext context)
    {
        var ct = context.CancellationToken;
        var userId = GetUserId(context);
        var ownerId = Guid.Parse(userId);

        var (provider, itemId) = SplitReference(request.ResourceRef);

        var owned = await dbContext.AlbumItems.AnyAsync(
            i => i.OwnerId == ownerId && i.Provider == provider && i.ProviderItemId == itemId, ct);

        if (!owned)
            return new GetPhotoContentReply { Exists = false };

        await foreach (var (client, connectionId) in GetReadableProvidersAsync(ct))
        {
            if (client.ServiceId != provider)
                continue;

            try
            {
                var location = await client.ResolveContentAsync(userId, connectionId, itemId, request.MaxSize, request.Refresh, ct);
                if (location == null)
                    continue;

                var reply = new GetPhotoContentReply
                {
                    Exists = true,
                    Url = location.Url,
                    ContentType = location.ContentType,
                    Size = location.Size,
                    Etag = location.ETag ?? "",
                    ResizeTo = location.ResizeTo,
                };

                foreach (var (name, value) in location.Headers)
                    reply.Headers[name] = value;

                return reply;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to resolve {ResourceRef} from {Service}", request.ResourceRef, provider);
            }
        }

        return new GetPhotoContentReply { Exists = false };
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
        var title = AlbumTitles.TryGetValue(category, out var known) ? known : category;
        var existing = await dbContext.Libraries.AsTracking()
            .FirstOrDefaultAsync(l => l.OwnerId == ownerId && l.Category == category);

        if (existing != null)
        {
            if (string.IsNullOrWhiteSpace(existing.Title))
            {
                existing.Title = title;
                existing.Updated = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            return;
        }

        var now = DateTimeOffset.UtcNow;
        dbContext.Libraries.Add(new SkyLibrary
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Category = category,
            Type = "Library",
            SharingLevel = "private",
            Title = title,
            Created = now,
            Updated = now,
        });

        await dbContext.SaveChangesAsync();
    }

    public override async Task<BeginPhotoUploadReply> BeginPhotoUpload(BeginPhotoUploadRequest request, ServerCallContext context)
    {
        var ct = context.CancellationToken;
        var metadata = request.Metadata
            ?? throw new RpcException(new Status(StatusCode.InvalidArgument, "Missing photo metadata."));

        if (request.ContentLength <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Refusing to upload an empty photo."));

        if (request.ContentLength > MaxUploadBytes)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Photo exceeds the maximum upload size."));

        var userId = GetUserId(context);
        var ownerId = Guid.Parse(userId);

        await EnsureLibraryAsync(ownerId, metadata.Category);

        var photo = ToUploadInfo(metadata, request.ContentLength);
        var reply = new BeginPhotoUploadReply();

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
                var albumId = await EnsureProviderAlbumAsync(ownerId, metadata.Category, proxyClient, userId, connection.Id, ct);
                var target = await proxyClient.BeginUploadAsync(userId, connection.Id, albumId, photo, ct);

                var descriptor = new UploadTarget
                {
                    Service = connection.Service,
                    ConnectionId = connection.Id,
                    Method = target.Method,
                    Url = target.Url,
                    FragmentSize = target.FragmentSize,
                };

                foreach (var (name, value) in target.Headers)
                    descriptor.Headers[name] = value;

                reply.Targets.Add(descriptor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to begin upload to {Service} ({ConnectionId})", connection.Service, connection.Id);
            }
        }

        if (reply.Targets.Count == 0)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "No photo-sync provider accepted the upload."));

        return reply;
    }

    public override async Task<PhotoReply> CompletePhotoUpload(CompletePhotoUploadRequest request, ServerCallContext context)
    {
        var ct = context.CancellationToken;
        var metadata = request.Metadata
            ?? throw new RpcException(new Status(StatusCode.InvalidArgument, "Missing photo metadata."));

        var userId = GetUserId(context);
        var ownerId = Guid.Parse(userId);

        var fileName = metadata.FileName;
        var mediaType = string.IsNullOrEmpty(metadata.MediaType) ? "photo" : metadata.MediaType;
        var photo = ToUploadInfo(metadata, 0);
        var reply = new PhotoReply { FileName = fileName, MediaType = mediaType };

        foreach (var outcome in request.Outcomes)
        {
            var proxyClient = photoSyncProxyClients.FirstOrDefault(p => p.ServiceId == outcome.Service);
            if (proxyClient == null)
                continue;

            if (outcome.StatusCode is < 200 or >= 300)
            {
                logger.LogError("Upload to {Service} ({ConnectionId}) failed with {Status}",
                                outcome.Service, outcome.ConnectionId, outcome.StatusCode);
                continue;
            }

            try
            {
                var albumId = await dbContext.ProviderAlbums
                    .Where(a => a.OwnerId == ownerId && a.Album == metadata.Category && a.Provider == outcome.Service)
                    .Select(a => a.ProviderAlbumId)
                    .FirstOrDefaultAsync(ct);

                if (string.IsNullOrEmpty(albumId))
                    continue;

                var result = await proxyClient.CompleteUploadAsync(userId, outcome.ConnectionId, albumId, photo, outcome.ResponseBody, ct);
                reply.ProviderItems.Add(new ProviderItem { Service = outcome.Service, ItemId = result.ItemId, Url = result.Url ?? "" });

                logger.LogInformation("Synced photo {FileName} to {Service} as {ItemId}", fileName, outcome.Service, result.ItemId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync photo to {Service} ({ConnectionId})", outcome.Service, outcome.ConnectionId);
            }
        }

        if (reply.ProviderItems.Count == 0)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "No photo-sync provider accepted the upload."));

        var now = DateTimeOffset.UtcNow;
        foreach (var item in reply.ProviderItems)
        {
            dbContext.AlbumItems.Add(new SkyAlbumItem
            {
                OwnerId = ownerId,
                Album = metadata.Category,
                Provider = item.Service,
                ProviderItemId = item.ItemId,
                SuppressNotification = metadata.SuppressNotification,
                Created = now,
            });
        }

        await dbContext.SaveChangesAsync(ct);

        var primary = reply.ProviderItems[0];
        reply.ResourceRef = $"{primary.Service}+{primary.ItemId}";
        return reply;
    }

    private static PhotoUploadInfo ToUploadInfo(PhotoUploadMetadata metadata, long length)
        => new(metadata.FileName, metadata.ContentType, metadata.Summary, length);

    private static readonly Dictionary<string, string> AlbumTitles = new()
    {
        ["wmphotos"] = "Windows phone photos",
        ["mobilephotos"] = "Mobile photos",
        ["twitterphotos"] = "Windows phone status photos",
    };

    private async Task<string> EnsureProviderAlbumAsync(Guid ownerId, string category, IPhotoSyncProxyClient client,
                                                        string userId, string connectionId, CancellationToken ct)
    {
        var existing = await dbContext.ProviderAlbums.AsTracking()
            .FirstOrDefaultAsync(a => a.OwnerId == ownerId && a.Album == category && a.Provider == client.ServiceId, ct);

        if (existing != null)
            return existing.ProviderAlbumId;

        var title = AlbumTitles.TryGetValue(category, out var known) ? known : category;
        var albumId = await client.EnsureAlbumAsync(userId, connectionId, title, ct);

        dbContext.ProviderAlbums.Add(new SkyProviderAlbum
        {
            OwnerId = ownerId,
            Album = category,
            Provider = client.ServiceId,
            ProviderAlbumId = albumId,
            Created = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Created {Service} album {AlbumId} for {Category}", client.ServiceId, albumId, category);

        return albumId;
    }

    private static (string Provider, string ItemId) SplitReference(string reference)
    {
        var separator = reference.IndexOf('+');
        if (separator <= 0 || separator == reference.Length - 1)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Malformed resource reference."));

        return (reference[..separator], reference[(separator + 1)..]);
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

    private static Library ToProto(SkyLibrary library, string coverRef = "") => new()
    {
        Id = library.Id.ToString(),
        Type = library.Type ?? "",
        Category = library.Category,
        SharingLevel = library.SharingLevel ?? "",
        Title = library.Title ?? "",
        Summary = library.Summary ?? "",
        EmailKeyword = library.EmailKeyword ?? "",
        CoverRef = coverRef,
    };
}
