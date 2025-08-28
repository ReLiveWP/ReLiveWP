namespace ReLiveWP.Services.Login.Models;

public record class ErrorModel(uint ErrorCode);
public record CreateAccountModel(string Username, string Password, string EmailAddress);
public record UserModel(string Id, string Cid, string Puid, string Username, string EmailAddress);
public record UserIdentityModel(string Id, string Cid, long Puid, string Username, string Password);
public record ProvisionDeviceRequestModel(string DeviceId, string Csr);
public record ProvisionDeviceResponseModel(UserIdentityModel Identity, SecurityTokenModel[] SecurityTokens, string DeviceCert);
public record ConnectionModel(string Id, string Url, string Name);
public record ConnectionModels(Dictionary<string, List<ConnectionModel>> Connections);
public record BeginAcountLinkModel(string Service, string? Identifier = null);
public record BeginAccountLinkResponse(string RedirectUri);
