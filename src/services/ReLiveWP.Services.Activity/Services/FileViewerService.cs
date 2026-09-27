using System.Globalization;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using ReLiveWP.Identity;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Services;

public class FileViewerService(User.UserClient userClient, IMemoryCache cache, ILogger<FileViewerService> logger)
{
    private static readonly TimeSpan OwnerCidLifetime = TimeSpan.FromMinutes(10);

    public async Task<long?> SubjectCidAsync(string id, ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (!long.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cid))
            return null;

        var ownerCid = await OwnerCidAsync(user, ct);
        if (ownerCid == null || cid == ownerCid)
            return null;

        logger.LogInformation("Files route addressed to {Cid}, owner is {Owner}", cid, ownerCid);
        return cid;
    }

    // a media ticket carries only the user id, every other token arrives with the cid in it
    public async Task<long?> OwnerCidAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (user.Cid() is { } claimed)
            return claimed;

        var userId = user.Id()!;
        var key = $"owner-cid:{userId}";
        if (cache.TryGetValue<long>(key, out var cached))
            return cached;

        try
        {
            var userInfo = await userClient.GetUserInfoAsync(new GetUserInfoRequest { UserId = userId }, cancellationToken: ct);

            var ownerCid = long.Parse(userInfo.Cid, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            cache.Set(key, ownerCid, OwnerCidLifetime);
            return ownerCid;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "could not read the owner cid, treating the files route as self");
            return null;
        }
    }
}
