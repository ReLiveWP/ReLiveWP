using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Backend.Identity;
using ReLiveWP.Identity;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Login.Models;

// {"credentials":{"ps:password":"asdf"},"identity":"wamwoowam@gmail.com","token_requests":[{"service_policy":"LEGACY","service_target":"http://Passport.NET/tb"}]}


namespace ReLiveWP.Services.Login.Controllers
{
    record class ErrorModel(uint ErrorCode);
    public record CreateAccountModel(string Username, string Password, string EmailAddress);
    public record UserModel(string Id, string Cid, string Puid, string Username, string EmailAddress);

    [ApiController]
    [Route("auth/[action]/{id?}")]
    public class AuthenticationController(
        ILogger<AuthenticationController> logger,
        Authentication.AuthenticationClient authenticationClient,
        User.UserClient userClient) : ControllerBase
    {
        [Authorize]
        [ActionName("user")]
        public async Task<ActionResult<UserModel>> GetUser(string id)
        {
            if (id != "@me")
                return Forbid();

            if (User == null)
                return Unauthorized();

            var user = await userClient.GetUserInfoAsync(new GetUserInfoRequest() { UserId = User.Id() });
            if (user == null)
                return NotFound(); // this is pretty bad, maybe 500 is better?

            return new UserModel(User.Id()!, user.Cid, user.Puid.ToString(), user.Username, user.EmailAddress);
        }


        [ActionName("register")]
        public async Task<IActionResult> RequestTokens([FromBody] CreateAccountModel request)
        {
            await authenticationClient.RegisterAsync(new RegisterRequest() { Username = request.Username, Password = request.Password, EmailAddress = request.EmailAddress });
            return NoContent();
        }

        [ActionName("request_tokens")]
        [HttpPost(Name = "request_tokens")]
        public async Task<ActionResult<SecurityTokensResponseModel>> RequestTokens([FromBody] SecurityTokensRequestModel request)
        {
            SecurityTokensRequest grpcRequest;
            if (request.Credentials.TryGetValue("ps:password", out var password))
            {
                grpcRequest = new SecurityTokensRequest()
                {
                    Username = request.Identity,
                    Password = password,
                };
            }
            else if (HttpContext.Request.Headers.TryGetValue("Authorization", out var values) && values.FirstOrDefault() != null)
            {
                var value = values.FirstOrDefault()!;
                if (value.StartsWith("Bearer "))
                    value = value[7..];

                grpcRequest = new SecurityTokensRequest()
                {
                    Username = request.Identity,
                    AuthToken = value
                };
            }
            else
            {
                return Unauthorized();
            }

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
            if ((grpcResponse.Code & 0x80000000) != 0)
            {
                return Unauthorized(new ErrorModel(grpcResponse.Code));
            }

            var securityTokens = new List<SecurityTokenModel>();
            foreach (var token in grpcResponse.Tokens)
            {
                securityTokens.Add(new SecurityTokenModel(token.ServiceTarget,
                                                          token.Token,
                                                          token.TokenType,
                                                          token.Created.ToDateTimeOffset(),
                                                          token.Expires.ToDateTimeOffset()));
            }

            return Ok(new SecurityTokensResponseModel(grpcResponse.Puid, grpcResponse.Cid, grpcResponse.Username, grpcResponse.EmailAddress, [.. securityTokens]));
        }
    }
}
