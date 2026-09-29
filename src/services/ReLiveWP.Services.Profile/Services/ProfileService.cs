using System.Globalization;
using System.ServiceModel;
using ReLiveWP.Identity.Soap;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;
using ReLiveWP.Services.Profile.Models;

namespace ReLiveWP.Services.Profile.Services;

[ServiceContract(Namespace = ProfileConstants.Ns)]
public interface IProfileService
{
    [OperationContract(Action = "http://profile.live.com/GetMany")]
    Task<GetManyResponse> GetMany(GetManyRequest message);
}

public class ProfileService(
    SoapTicketVerifier tickets,
    MailboxStore.MailboxStoreClient mailbox,
    User.UserClient users,
    IConfiguration configuration,
    ILogger<ProfileService> logger) : IProfileService
{
    private const string NoAvatar = "";

    private static readonly string[] TicketTargets =
    [
        "directory.services.live.com",
        "directory.services.live-int.com",
        "contacts.relivewp.net",
        "contacts.int.relivewp.net",
        "directory.relivewp.net",
    ];

    public async Task<GetManyResponse> GetMany(GetManyRequest message)
    {
        var ticket = await tickets.VerifyAsync(message.UserHeader?.TicketToken, TicketTargets);
        if (!ticket.IsValid)
            throw new FaultException(ticket.FailureMessage);

        var fallbackName = ticket.Claim("preferred_username")
            ?? EmailLocalPart(ticket.Claim("email") ?? FindEmail(ticket.Claims))
            ?? "User";

        var userId = ticket.UserId;
        var owner = await LoadOwnerAsync(userId);
        var cidByProfile = message.Request.Ids.ToDictionary(id => id, ExtractCid);

        var wantedCids = cidByProfile.Values.Where(c => c.HasValue).Select(c => c!.Value).Distinct().ToList();
        var profilesByCid = new Dictionary<long, ContactProfile>();
        if (wantedCids.Count > 0)
        {
            var lookup = await mailbox.GetContactProfilesAsync(new GetContactProfilesRequest
            {
                UserId = userId,
                Cids = { wantedCids },
            });
            foreach (var p in lookup.Profiles)
                profilesByCid[p.Cid] = p;

            logger.LogInformation(
                "Profile GetMany: {Requested} ids, {Parsed} CIDs parsed, {Resolved} resolved to contacts",
                message.Request.Ids.Count, wantedCids.Count, lookup.Profiles.Count);
        }

        var response = new GetManyResponse();
        foreach (var id in message.Request.Ids)
        {
            var cid = cidByProfile[id];

            ProfileView view;
            if (owner is not null && cid == owner.Cid)
                view = BuildView(owner.FirstName, owner.LastName, owner.TileUrl ?? NoAvatar);
            else if (cid is { } c && profilesByCid.TryGetValue(c, out var contact))
                view = BuildView(contact.DisplayName, "", string.IsNullOrEmpty(contact.AvatarUrl) ? NoAvatar : contact.AvatarUrl);
            else
                view = BuildView(fallbackName, "", NoAvatar);

            response.GetManyResult.Profiles.Add(new ProfileResponse { ProfileId = id, View = view });
        }

        return response;
    }

    private record Owner(long Cid, string FirstName, string LastName, string? TileUrl);

    private async Task<Owner?> LoadOwnerAsync(string userId)
    {
        try
        {
            var profile = await users.GetUserProfileAsync(new GetUserProfileRequest { UserId = userId });
            if (!long.TryParse(profile.Cid, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var cid))
                return null;

            var first = profile.HasFirstName ? profile.FirstName : null;
            var last = profile.HasLastName ? profile.LastName : null;
            var tile = profile.HasPictureEtag ? TileUrl(profile.Cid) : null;

            return new Owner(cid, first ?? profile.Username, last ?? "", tile);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "could not read the owner profile for {User}", userId);
            return null;
        }
    }

    private string? TileUrl(string cid)
    {
        var root = configuration["PublicUrls:Avatars"];
        return string.IsNullOrWhiteSpace(root) ? null : $"{root.TrimEnd('/')}/{cid}/picture";
    }

    private static ProfileView BuildView(string displayName, string lastName, string avatarUrl) => new()
    {
        Attributes =
        [
            new(ProfileConstants.ExprDisplayName, new ProfileValue(displayName)),
            new(ProfileConstants.ExprDisplayLastName, new ProfileValue(lastName)),
            new(ProfileConstants.ExprUserTileUrl, new ProfileValue(avatarUrl)),
            new(ProfileConstants.ExprUserTileLastModified, new ProfileValue(DateTime.UtcNow)),
        ]
    };

    private long? ExtractCid(ProfileId id)
    {
        logger.LogDebug("ProfileId slots: [{Ns1}={V1}] [{Ns2}={V2}] [{Ns3}={V3}]",
            id.Ns1, id.V1?.Value, id.Ns2, id.V2?.Value, id.Ns3, id.V3?.Value);

        foreach (var v in new[] { id.V1?.Value, id.V2?.Value, id.V3?.Value })
        {
            if (!string.IsNullOrWhiteSpace(v) && long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cid))
                return cid;
        }

        return null;
    }

    private static string? FindEmail(IReadOnlyDictionary<string, string> claims) =>
        claims.FirstOrDefault(c => c.Key.EndsWith("email", StringComparison.OrdinalIgnoreCase) ||
                                   c.Key.EndsWith("emailaddress", StringComparison.OrdinalIgnoreCase)).Value;

    private static string? EmailLocalPart(string? email)
    {
        if (string.IsNullOrEmpty(email)) return null;
        var at = email.IndexOf('@');
        return at > 0 ? email[..at] : email;
    }
}
