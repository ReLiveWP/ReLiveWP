using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Backend.Identity.Services;
using ReLiveWP.ServiceDefaults.Events;
using ReLiveWP.Services.Grpc;
using StackExchange.Redis;

namespace ReLiveWP.Backend.Identity.Grpc;

public class UserService(
    UserManager<LiveUser> userManager,
    LiveDbContext dbContext,
    AvatarStore avatarStore,
    AvatarProcessor avatarProcessor,
    IConnectionMultiplexer redis,
    ILogger<UserService> logger) : User.UserBase
{
    public override async Task<GetUserInfoResponse> GetUserInfo(GetUserInfoRequest request, ServerCallContext context)
    {
        var user = await userManager.FindByIdAsync(request.UserId)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "User does not exist."));

        var profile = await FindProfileAsync(user.Id, context.CancellationToken);

        var response = new GetUserInfoResponse()
        {
            Cid = user.Cid,
            Puid = user.Puid,
            Username = user.UserName,
            EmailAddress = user.Email
        };

        if (profile?.FirstName is { } first) response.FirstName = first;
        if (profile?.LastName is { } last) response.LastName = last;
        if (profile?.PictureEtag is { } etag) response.PictureEtag = etag;
        if (profile?.ProfileVisibility is { } visibility) response.Visibility = (ProfileVisibility)visibility;

        return response;
    }

    public override async Task ListUsers(ListUsersRequest request, IServerStreamWriter<UserSummary> stream, ServerCallContext context)
    {
        var query = userManager.Users
            .Where(u => u.Type == LiveUserType.User)
            .Include(u => u.Profile);
        await foreach (var user in query.AsAsyncEnumerable().WithCancellation(context.CancellationToken))
        {
            await stream.WriteAsync(new UserSummary
            {
                Id = user.Id.ToString(),
                Username = user.UserName ?? "",
                EmailAddress = user.Email ?? "",
                Visibility = (ProfileVisibility)(user.Profile?.ProfileVisibility ?? LiveProfileVisibility.Private)
            });
        }
    }

    public override async Task<LookupUsersByEmailResponse> LookupUsersByEmail(LookupUsersByEmailRequest request, ServerCallContext context)
    {
        const int MaxEmails = 100;

        if (request.Emails.Count > MaxEmails)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"at most {MaxEmails} addresses per lookup."));

        var queried = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var email in request.Emails)
        {
            if (string.IsNullOrWhiteSpace(email))
                continue;

            if (userManager.NormalizeEmail(email) is { } key && !queried.ContainsKey(key))
                queried[key] = email;
        }

        var response = new LookupUsersByEmailResponse();
        if (queried.Count == 0)
            return response;

        var keys = queried.Keys.ToList();
        var users = await userManager.Users
            .Where(u => u.Type == LiveUserType.User && u.NormalizedEmail != null && keys.Contains(u.NormalizedEmail))
            .Include(u => u.Profile)
            .ToListAsync(context.CancellationToken);

        foreach (var user in users.GroupBy(u => u.NormalizedEmail!).Select(g => g.OrderBy(u => u.Id).First()))
        {
            var visibility = user.Profile?.ProfileVisibility ?? LiveProfileVisibility.Private;
            if (visibility == LiveProfileVisibility.Private && !request.IncludePrivate)
                continue;

            var entry = new DirectoryUser
            {
                QueriedEmail = queried[user.NormalizedEmail!],
                UserId = user.Id.ToString(),
                Cid = user.Cid,
                EmailAddress = user.Email ?? "",
                Visibility = (ProfileVisibility)visibility,
            };

            if (user.Profile?.PictureEtag is { } etag)
                entry.PictureEtag = etag;

            response.Users.Add(entry);
        }

        return response;
    }

    public override async Task<UserProfile> GetUserProfile(GetUserProfileRequest request, ServerCallContext context)
    {
        var user = await FindUserAsync(request.UserId);
        return ToProto(user, await FindProfileAsync(user.Id, context.CancellationToken));
    }

    public override async Task<UserProfile> UpdateUserProfile(UpdateUserProfileRequest request, ServerCallContext context)
    {
        var user = await FindUserAsync(request.UserId);
        var profile = await GetOrCreateProfileAsync(user.Id, context.CancellationToken);

        var first = request.HasFirstName ? Trim(request.FirstName) : profile.FirstName;
        var last = request.HasLastName ? Trim(request.LastName) : profile.LastName;
        var visibility = request.HasVisibility
            ? (LiveProfileVisibility)request.Visibility
            : profile.ProfileVisibility;

        if (first == profile.FirstName && last == profile.LastName && visibility == profile.ProfileVisibility)
            return ToProto(user, profile);

        profile.FirstName = first;
        profile.LastName = last;
        profile.ProfileVisibility = visibility;
        profile.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(context.CancellationToken);

        await PublishAsync(user, profile);
        return ToProto(user, profile);
    }

    public override async Task<UserProfile> SetUserPicture(SetUserPictureRequest request, ServerCallContext context)
    {
        var user = await FindUserAsync(request.UserId);
        var profile = await GetOrCreateProfileAsync(user.Id, context.CancellationToken);

        var crop = request.Crop is { } c ? new AvatarCrop(c.X, c.Y, c.Size) : (AvatarCrop?)null;

        ProcessedAvatar processed;
        try
        {
            processed = await avatarProcessor.ProcessAsync(request.Data.ToByteArray(), crop, context.CancellationToken);
        }
        catch (AvatarProcessingException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }

        var etag = avatarStore.ComputeEtag(processed.Original);
        var fileName = await avatarStore.WriteAsync(etag, processed.Extension, processed.Original, context.CancellationToken);

        var previous = profile.PictureFileName;

        profile.PictureFileName = fileName;
        profile.PictureContentType = processed.ContentType;
        profile.PictureEtag = etag;
        profile.PictureThumbnail = processed.Thumbnail;
        profile.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(context.CancellationToken);

        if (previous is not null && previous != fileName)
            TryDelete(previous);

        await PublishAsync(user, profile);
        return ToProto(user, profile);
    }

    public override async Task<UserProfile> ClearUserPicture(ClearUserPictureRequest request, ServerCallContext context)
    {
        var user = await FindUserAsync(request.UserId);
        var profile = await FindProfileAsync(user.Id, context.CancellationToken);
        if (profile?.PictureFileName is null)
            return ToProto(user, profile);

        var previous = profile.PictureFileName;

        profile.PictureFileName = null;
        profile.PictureContentType = null;
        profile.PictureEtag = null;
        profile.PictureThumbnail = null;
        profile.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(context.CancellationToken);

        TryDelete(previous);

        await PublishAsync(user, profile);
        return ToProto(user, profile);
    }

    public override async Task<GetUserPictureResponse> GetUserPicture(GetUserPictureRequest request, ServerCallContext context)
    {
        var profile = request.OwnerCase switch
        {
            GetUserPictureRequest.OwnerOneofCase.Cid => await dbContext.LiveUserProfiles
                .Where(p => dbContext.Users.Any(u => u.Id == p.UserId && u.Cid == request.Cid))
                .SingleOrDefaultAsync(context.CancellationToken),
            _ => await FindProfileAsync((await FindUserAsync(request.UserId)).Id, context.CancellationToken),
        };

        if (profile?.PictureFileName is null || profile.PictureEtag is null)
            throw new RpcException(new Status(StatusCode.NotFound, "No picture set."));

        if (request.Thumbnail)
        {
            if (profile.PictureThumbnail is null)
                throw new RpcException(new Status(StatusCode.NotFound, "No picture set."));

            return new GetUserPictureResponse
            {
                Data = ByteString.CopyFrom(profile.PictureThumbnail),
                ContentType = profile.PictureContentType ?? "image/jpeg",
                Etag = profile.PictureEtag,
            };
        }

        var data = await avatarStore.ReadAsync(profile.PictureFileName, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Picture file is missing."));

        return new GetUserPictureResponse
        {
            Data = ByteString.CopyFrom(data),
            ContentType = profile.PictureContentType ?? "image/jpeg",
            Etag = profile.PictureEtag,
        };
    }

    private async Task<LiveUser> FindUserAsync(string userId) =>
        await userManager.FindByIdAsync(userId)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "User does not exist."));

    private Task<LiveUserProfile?> FindProfileAsync(Guid userId, CancellationToken ct) =>
        dbContext.LiveUserProfiles.SingleOrDefaultAsync(p => p.UserId == userId, ct);

    private async Task<LiveUserProfile> GetOrCreateProfileAsync(Guid userId, CancellationToken ct)
    {
        var profile = await FindProfileAsync(userId, ct);
        if (profile is not null)
            return profile;

        profile = new LiveUserProfile { UserId = userId, UpdatedAt = DateTime.UtcNow };
        dbContext.LiveUserProfiles.Add(profile);
        return profile;
    }

    private void TryDelete(string fileName)
    {
        try
        {
            avatarStore.Delete(fileName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "could not delete superseded avatar {File}", fileName);
        }
    }

    private async Task PublishAsync(LiveUser user, LiveUserProfile? profile)
    {
        try
        {
            await redis.PublishProfileChangedAsync(new AccountProfileChangedEvent(
                user.Id.ToString(),
                user.Cid,
                profile?.FirstName,
                profile?.LastName,
                user.UserName ?? "",
                user.Email ?? "",
                profile?.PictureEtag,
                (profile?.ProfileVisibility ?? LiveProfileVisibility.Private).ToString()));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "failed to publish account.profile-changed for {User}", user.Id);
        }
    }

    private static string? Trim(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static UserProfile ToProto(LiveUser user, LiveUserProfile? profile)
    {
        var proto = new UserProfile
        {
            UserId = user.Id.ToString(),
            Cid = user.Cid,
            Puid = user.Puid,
            Username = user.UserName ?? "",
            EmailAddress = user.Email ?? "",
        };

        if (profile?.FirstName is { } first) proto.FirstName = first;
        if (profile?.LastName is { } last) proto.LastName = last;
        if (profile?.PictureEtag is { } etag) proto.PictureEtag = etag;
        if (profile?.ProfileVisibility is { } visibility) proto.Visibility = (ProfileVisibility)visibility;

        return proto;
    }
}
