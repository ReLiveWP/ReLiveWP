namespace ReLiveWP.Services.Login.Models;

public record InlineLoginRequestModel(
    string Identity,
    Dictionary<string, string> Credentials);

public record InlineLoginResponseModel(
    string DaToken,
    string DaSessionKey,
    string DaStartTime,
    string DaExpires,
    string StsInlineFlowToken,
    string Cid,
    string Puid,
    string Username,
    string? FirstName,
    string? LastName);
