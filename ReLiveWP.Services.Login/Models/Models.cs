using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Login.Models;

public record class ErrorModel(uint ErrorCode, string? HelpUrl = null);

public record CreateAccountModel(
    string Username,
    string Password, 
    string EmailAddress);

public record CreateDeviceAccountModel(
    string Username, 
    string Password, 
    string DeviceId);

public record CreateDeviceAccountResponseModel(UserModel Identity, SecurityTokenModel[] SecurityTokens);

public record UserModel(
    string Id,
    string Cid,
    long Puid,
    string Username,
    string EmailAddress,
    string? FirstName = null,
    string? LastName = null,
    string? PictureUrl = null,
    string? PictureEtag = null,
    string Visibility = "private");

public record UpdateProfileModel(string? FirstName, string? LastName, string? Visibility = null);

public static class VisibilityModel
{
    public static string ToModel(ProfileVisibility visibility) => visibility switch
    {
        ProfileVisibility.Mutual => "mutual",
        ProfileVisibility.Public => "public",
        _ => "private",
    };

    public static ProfileVisibility? FromModel(string? value) => value?.ToLowerInvariant() switch
    {
        "private" => ProfileVisibility.Private,
        "mutual" => ProfileVisibility.Mutual,
        "public" => ProfileVisibility.Public,
        _ => null,
    };
}

public record ProvisionDeviceRequestModel(string Csr);

public record ProvisionDeviceResponseModel(string DeviceCert);

public record ConnectionModel(
    string Id,
    string Url,
    string Name,
    bool NeedsRelink,
    uint EnabledCapabilities,
    uint SharedCapabilities = 0);

public record ConnectionModels(Dictionary<string, List<ConnectionModel>> Connections);

public record BeginAcountLinkModel(string Service, string? Identifier = null, bool Transient = false);

public record BeginRelinkModel(string ConnectionId, uint? RequestedCapabilities = null);

public record BeginAccountLinkResponse(string RedirectUri);

public record CredentialLinkModel(
    string Service,
    string ServiceUrl,
    string Username,
    string Secret,
    string? ConnectionId = null,
    string? Label = null,
    bool Transient = false);

public record CredentialLinkResponse(string ConnectionId);

public record DeleteLinkModel(string ConnectionId);

public record ConnectedDeviceModel(
    string FriendlyName, 
    string? Manufacturer,
    string? Model, 
    string? Operator,
    string? PhoneNumber, 
    string OSVersion,
    string Locale);

public record AvailableConnectedService(
    string Service,
    string DisplayName,
    uint Capabilities,
    uint ShareableCapabilities = 0
);

public record UpdateConnectedServiceModel(
    uint EnabledCapabilities,
    uint? SharedCapabilities = null
);