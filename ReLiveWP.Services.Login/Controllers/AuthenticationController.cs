using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Backend.Identity;
using ReLiveWP.Services.Login.Models;

// {"credentials":{"ps:password":"asdf"},"identity":"wamwoowam@gmail.com","token_requests":[{"service_policy":"LEGACY","service_target":"http://Passport.NET/tb"}]}

namespace ReLiveWP.Services.Login.Controllers
{
    [ApiController]
    [Route("auth/[action]")]
    public class AuthenticationController(
        ILogger<AuthenticationController> logger,
        Authentication.AuthenticationClient authenticationClient) : ControllerBase
    {
        [ActionName("request_tokens")]
        [HttpPost(Name = "request_tokens")]
        public async Task<SecurityTokensResponseModel> RequestTokens([FromBody] SecurityTokensRequestModel request)
        {
            var grpcRequest = new SecurityTokensRequest()
            {
                Username = request.Identity,
                Password = request.Credentials["ps:password"],
            };

            foreach (var tokenRequest in request.TokenRequests)
            {
                var grpcTokenRequest = new SecurityTokenRequest()
                {
                    ServicePolicy = tokenRequest.ServicePolicy,
                    ServiceTarget = tokenRequest.ServiceTarget
                };

                grpcRequest.Requests.Add(grpcTokenRequest);
            }

            var grpcResponse = await authenticationClient.GetSecurityTokensAsync(grpcRequest);
            Marshal.ThrowExceptionForHR((int)grpcResponse.Code);

            var securityTokens = new List<SecurityTokenModel>();
            foreach (var token in grpcResponse.Tokens)
            {
                securityTokens.Add(new SecurityTokenModel(token.ServiceTarget,
                                                          token.Token,
                                                          token.TokenType,
                                                          token.Created.ToDateTimeOffset(),
                                                          token.Expires.ToDateTimeOffset()));
            }

            return new SecurityTokensResponseModel(grpcResponse.Puid, grpcResponse.Cid, grpcResponse.Username, grpcResponse.EmailAddress, [.. securityTokens]);
        }
    }
}
