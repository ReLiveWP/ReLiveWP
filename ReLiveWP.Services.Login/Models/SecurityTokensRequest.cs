// {"credentials":{"ps:password":"asdf"},"identity":"wamwoowam@gmail.com","token_requests":[{"service_policy":"LEGACY","service_target":"http://Passport.NET/tb"}]}

namespace ReLiveWP.Services.Login.Models;

public record SecurityTokensRequestModel(string Identity, Dictionary<string, string> Credentials, List<SecurityTokenRequestModel> TokenRequests);
