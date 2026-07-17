using System.Globalization;
using System.ServiceModel;
using System.Web;
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
    Authentication.AuthenticationClient authentication,
    MailboxStore.MailboxStoreClient mailbox,
    ILogger<ProfileService> logger) : IProfileService
{
    // Fallback avatar for CIDs we can't resolve to one of the caller's contacts (unlinked river
    // authors, etc.). Real contacts use their own stored tile.
    private const string PlaceholderAvatar =
        "https://cdn.bsky.app/img/avatar_thumbnail/plain/did:plc:7rfssi44thh6f4ywcl3u5nvt/bafkreifzzvtmxjraoiym6plysmh3e5wc257aecnapxvs427esswjktmvoy@jpeg";

    public async Task<GetManyResponse> GetMany(GetManyRequest message)
    {
        var token = message.UserHeader?.TicketToken;
        if (string.IsNullOrWhiteSpace(token))
            throw new FaultException("Missing TicketToken.");

        var parser = HttpUtility.ParseQueryString(token);

        // TODO: there is 100% a better way to do this, i refuse to believe SoapCore doesn't
        // offer an auth mechanism of its own
        var verify = new VerifyTokenRequest { Token = parser["t"], TokenType = "JWT" };
        verify.ServiceTargets.Add("directory.services.live.com");
        verify.ServiceTargets.Add("directory.services.live-int.com");
        verify.ServiceTargets.Add("contacts.relivewp.net");
        verify.ServiceTargets.Add("contacts.int.relivewp.net");

        VerifyResponse reply;
        try
        {
            reply = await authentication.VerifySecurityTokenAsync(verify);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate TicketToken.");
            throw new FaultException("Authentication service error.");
        }

        if (reply.Code != 0)
        {
            logger.LogWarning("TicketToken validation failed with code {Code:X}.", reply.Code);
            throw new FaultException("Invalid TicketToken.");
        }

        var claims = reply.Claims.ToDictionary(c => c.Type, c => c.Value);
        var fallbackName = Claim(claims, "preferred_username")
            ?? EmailLocalPart(Claim(claims, "email") ?? FindEmail(claims))
            ?? "User";

        // reply.Id is the caller's account id (NameIdentifier), which is the mailbox user_id scope.
        var userId = reply.Id;

        // Requested CID per ProfileId. The wire slot carrying the CID isn't fully pinned down yet,
        // so scan the id's value slots for the first parseable int64 and log the raw shape for a
        // device capture. Unparseable ids resolve to null and degrade to the placeholder.
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
            var view = cidByProfile[id] is { } cid && profilesByCid.TryGetValue(cid, out var contact)
                ? BuildView(contact.DisplayName, string.IsNullOrEmpty(contact.AvatarUrl) ? PlaceholderAvatar : contact.AvatarUrl)
                : BuildView(fallbackName, PlaceholderAvatar);

            response.GetManyResult.Profiles.Add(new ProfileResponse { ProfileId = id, View = view });
        }

        return response;
    }

    private static ProfileView BuildView(string displayName, string avatarUrl) => new()
    {
        Attributes =
        [
            new(ProfileConstants.ExprDisplayName, new ProfileValue(displayName)),
            new(ProfileConstants.ExprDisplayLastName, new ProfileValue("")),
            new(ProfileConstants.ExprUserTileUrl, new ProfileValue(avatarUrl)),
            new(ProfileConstants.ExprUserTileLastModified, new ProfileValue(DateTime.UtcNow)),
        ]
    };

    // Pull the contact CID out of a ProfileId. The device sends it in one of the value slots as a
    // decimal int64; take the first that parses. Logs the raw slots so the exact slot can be pinned.
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

    private static string? Claim(Dictionary<string, string> claims, string type) =>
        claims.TryGetValue(type, out var v) && !string.IsNullOrEmpty(v) ? v : null;

    private static string? FindEmail(Dictionary<string, string> claims) =>
        claims.FirstOrDefault(c => c.Key.EndsWith("email", StringComparison.OrdinalIgnoreCase) ||
                                   c.Key.EndsWith("emailaddress", StringComparison.OrdinalIgnoreCase)).Value;

    private static string? EmailLocalPart(string? email)
    {
        if (string.IsNullOrEmpty(email)) return null;
        var at = email.IndexOf('@');
        return at > 0 ? email[..at] : email;
    }
}
