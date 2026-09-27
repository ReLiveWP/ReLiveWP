
namespace ReLiveWP.Services.Login.Models;

public record SecurityTokensResponseModel(
    long Puid,
    string Cid,
    string Username,
    string EmailAddress,
    SecurityTokenModel[] SecurityTokens);
