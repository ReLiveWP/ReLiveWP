// {"credentials":{"ps:password":"asdf"},"identity":"wamwoowam@gmail.com","token_requests":[{"service_policy":"LEGACY","service_target":"http://Passport.NET/tb"}]}

namespace ReLiveWP.Services.Login.Models;

public record SecurityTokensResponseModel(ulong Puid, string Cid, string Username, string EmailAddress, SecurityTokenModel[] SecurityTokens);
