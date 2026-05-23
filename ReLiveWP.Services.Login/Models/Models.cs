namespace ReLiveWP.Services.Login.Models;

public record class ErrorModel(uint ErrorCode);
public record CreateAccountModel(string Username, string Password, string EmailAddress);
public record CreateDeviceAccountModel(string Username, string Password, string DeviceId);
public record CreateDeviceAccountResponseModel(UserModel Identity, SecurityTokenModel[] SecurityTokens);
public record UserModel(string Id, string Cid, long Puid, string Username, string EmailAddress);
public record ProvisionDeviceRequestModel(string Csr);
public record ProvisionDeviceResponseModel(string DeviceCert);
public record ConnectionModel(string Id, string Url, string Name);
public record ConnectionModels(Dictionary<string, List<ConnectionModel>> Connections);
public record BeginAcountLinkModel(string Service, string? Identifier = null);
public record BeginAccountLinkResponse(string RedirectUri);
