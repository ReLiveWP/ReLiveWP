namespace ReLiveWP.Services.Login.Models;

public record class ErrorModel(uint ErrorCode);

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
    string EmailAddress);

public record ProvisionDeviceRequestModel(string Csr);

public record ProvisionDeviceResponseModel(string DeviceCert);

public record ConnectionModel(
    string Id, 
    string Url, 
    string Name, 
    bool NeedsRelink);

public record ConnectionModels(Dictionary<string, List<ConnectionModel>> Connections);

public record BeginAcountLinkModel(string Service, string? Identifier = null);

public record BeginRelinkModel(string ConnectionId);

public record BeginAccountLinkResponse(string RedirectUri);

public record DeleteLinkModel(string ConnectionId);

public record ConnectedDeviceModel(
    string FriendlyName, 
    string? Manufacturer,
    string? Model, 
    string? Operator,
    string? PhoneNumber, 
    string OSVersion,
    string Locale);