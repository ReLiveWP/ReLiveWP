using System.ServiceModel;
using Grpc.Core;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Profile.Models;
using SoapCore;

namespace ReLiveWP.Services.Profile.Services;

[ServiceContract(Namespace = ProfileConstants.Ns)]
public interface IProfileService
{
    [OperationContract(Action = "http://profile.live.com/GetMany")]
    Task<GetManyResponse> GetMany(GetManyRequest message);
}

public class ProfileService(
    Authentication.AuthenticationClient authentication,
    ILogger<ProfileService> logger) : IProfileService
{
    public async Task<GetManyResponse> GetMany(GetManyRequest message)
    {
        var token = message.UserHeader?.TicketToken;
        if (string.IsNullOrWhiteSpace(token))
            throw new FaultException("Missing TicketToken.");

        // TODO: there is 100% a better way to do this, i refuse to believe SoapCore doesn't
        // offer an auth mechanism of its own
        var verify = new VerifyTokenRequest { Token = token, TokenType = "JWT" };
        verify.ServiceTargets.Add("directory.services.live.com");
        verify.ServiceTargets.Add("contacts.relivewp.net");

        VerifyTokenResponse reply;
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
        var displayName = Claim(claims, "preferred_username")
            ?? EmailLocalPart(Claim(claims, "email") ?? FindEmail(claims))
            ?? "User";

        return BuildResponse(message, displayName);
    }

    // placeholder profile data for now (last name/avatar not stored yet)
    private static GetManyResponse BuildResponse(GetManyRequest message, string displayName)
    {
        var response = new GetManyResponse();
        foreach (var id in message.Request.Ids)
        {
            response.GetManyResult.Profiles.Add(new ProfileResponse
            {
                ProfileId = id,
                View = new ProfileView
                {
                    Attributes =
                    [
                        new(ProfileConstants.ExprDisplayName, new ProfileValue(displayName)),
                        new(ProfileConstants.ExprDisplayLastName, new ProfileValue("")),
                        new(ProfileConstants.ExprUserTileUrl, new ProfileValue("https://cdn.bsky.app/img/avatar_thumbnail/plain/did:plc:7rfssi44thh6f4ywcl3u5nvt/bafkreifzzvtmxjraoiym6plysmh3e5wc257aecnapxvs427esswjktmvoy@jpeg")),
                        new(ProfileConstants.ExprUserTileLastModified, new ProfileValue(DateTime.UtcNow)),
                    ]
                }
            });
        }

        return response;
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
