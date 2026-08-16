using System.Globalization;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Services;

public class FilesViewer(User.UserClient userClient, ILogger<FilesViewer> logger)
{
    public async Task<long?> SubjectCidAsync(string id, string userId, CancellationToken ct = default)
    {
        if (!long.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cid))
            return null;

        try
        {
            var userInfo = await userClient.GetUserInfoAsync(new GetUserInfoRequest { UserId = userId }, cancellationToken: ct);

            var ownerCid = long.Parse(userInfo.Cid, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (cid == ownerCid)
                return null;

            logger.LogInformation("Files route addressed to {Cid}, owner is {Owner}", cid, ownerCid);
            return cid;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "could not read the owner cid, treating the files route as self");
            return null;
        }
    }
}
