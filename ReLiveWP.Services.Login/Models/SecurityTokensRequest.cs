
namespace ReLiveWP.Services.Login.Models;

public record SecurityTokensRequestModel(
    string Identity,
    Dictionary<string, string> Credentials, 
    List<SecurityTokenRequestModel> TokenRequests);
