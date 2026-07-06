using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Login.Models;

namespace ReLiveWP.Services.Login.Controllers;

[ApiController]
[Route("auth/[action]/{id?}")]
public class AuthenticationController(
    User.UserClient userClient,
    Authentication.AuthenticationClient authenticationClient,
    ConnectedServices.ConnectedServicesClient connectedServicesClient) : ControllerBase
{
    [Authorize]
    [ActionName("user")]
    public async Task<ActionResult<UserModel>> GetUser(string id, CancellationToken cancellationToken)
    {
        if (id != "@me")
            return Forbid();

        if (User == null)
            return Unauthorized();

        var user = await userClient.GetUserInfoAsync(new GetUserInfoRequest() { UserId = User.Id() }, cancellationToken: cancellationToken);
        if (user == null)
            return NotFound();

        return new UserModel(User.Id()!, user.Cid, user.Puid, user.Username, user.EmailAddress);
    }

    [Authorize]
    [Route("/auth/user/@me/linked-accounts")]
    public async Task<ActionResult<ConnectionModels>> GetLinkedAccounts(CancellationToken cancellationToken)
    {
        var connections = connectedServicesClient.GetConnections(new ConnectionsRequest(), cancellationToken: cancellationToken);

        var connectionModels = new Dictionary<string, List<ConnectionModel>>();
        await foreach (var connection in connections.ResponseStream.ReadAllAsync(cancellationToken))
        {
            if (!connectionModels.TryGetValue(connection.Service, out var connectionList))
                connectionModels[connection.Service] = connectionList = [];

            connectionList.Add(new ConnectionModel(
                connection.Id,
                connection.ServiceUrl,
                connection.UserName,
                (connection.Flags & 0x80000000UL) != 0,
                connection.Capabilities));
        }

        return new ConnectionModels(connectionModels);
    }

    [ActionName("register")]
    public async Task<IActionResult> CreateAccountAsync([FromBody] CreateAccountModel request)
    {
        await authenticationClient.RegisterAsync(new RegisterRequest()
        {
            Username = request.Username,
            Password = request.Password,
            EmailAddress = request.EmailAddress
        });

        return Created();
    }

    [ActionName("request_tokens")]
    [HttpPost(Name = "request_tokens")]
    public async Task<ActionResult<SecurityTokensResponseModel>> RequestTokens([FromBody] SecurityTokensRequestModel request, CancellationToken cancellationToken)
    {
        // TODO oh boy howdy this needs to go away
        var grpcRequest = new SecurityTokensRequest();
        if (request.Credentials.TryGetValue("ps:password", out var password))
        {
            grpcRequest.Username = request.Identity;
            grpcRequest.Password = password;
        }
        // TODO: i feel like we might be able to remove this path at some stage
        else if (HttpContext.Request.Headers.TryGetValue("Authorization", out var values) && values.FirstOrDefault() != null)
        {
            var value = values.FirstOrDefault()!;
            if (value.StartsWith("Bearer "))
                value = value[7..];

            grpcRequest.Username = request.Identity;
            grpcRequest.AuthToken = value;
        }
        else
        {
            return Unauthorized();
        }

        grpcRequest.IssueRefreshToken = request.IncludeRefreshToken;

        foreach (var tokenRequest in request.TokenRequests)
        {
            grpcRequest.Requests.Add(new SecurityTokenRequest()
            {
                ServicePolicy = tokenRequest.ServicePolicy,
                ServiceTarget = tokenRequest.ServiceTarget
            });
        }

        // note: the SPA deliberately does not opt into refresh tokens (IssueRefreshToken stays false)
        var response = await authenticationClient.GetSecurityTokensAsync(grpcRequest, cancellationToken: cancellationToken);
        if (((int)response.Code) < 0)
            return Unauthorized(new ErrorModel(response.Code));

        return Ok(ToModel(response));
    }

    [ActionName("refresh_tokens")]
    [HttpPost(Name = "refresh_tokens")]
    public async Task<ActionResult<SecurityTokensResponseModel>> RefreshTokens([FromBody] RefreshTokensRequestModel request, CancellationToken cancellationToken)
    {
        var grpcRequest = new RefreshTokensRequest();
        foreach (var token in request.RefreshTokens)
            grpcRequest.RefreshTokens.Add(token);

        var response = await authenticationClient.RefreshSecurityTokensAsync(
            grpcRequest, cancellationToken: cancellationToken);
        if (((int)response.Code) < 0)
            return Unauthorized(new ErrorModel(response.Code));

        return Ok(ToModel(response));
    }

    private static SecurityTokensResponseModel ToModel(SecurityTokensResponse response)
    {
        var securityTokens = response.Tokens.Select(token =>
            new SecurityTokenModel(token.ServiceTarget,
                                   token.Token,
                                   token.TokenType,
                                   token.Created.ToDateTimeOffset(),
                                   token.Expires.ToDateTimeOffset(),
                                   token.HasRefreshToken ? token.RefreshToken : null,
                                   token.RefreshTokenExpires?.ToDateTimeOffset()));

        return new SecurityTokensResponseModel(
            response.Puid,
            response.Cid,
            response.Username,
            response.EmailAddress,
            [.. securityTokens]);
    }

    [ActionName("register_device")]
    [HttpPost(Name = "register_device")]
    public async Task<ActionResult<CreateDeviceAccountResponseModel>> RegisterDevice([FromBody] CreateDeviceAccountModel request)
    {
        var response = await authenticationClient.RegisterDeviceAsync(new RegisterDeviceRequest()
        {
            DeviceId = request.DeviceId,
            Username = request.Username,
            Password = request.Password,
            Requests =
            {
                new SecurityTokenRequest()
                {
                    ServiceTarget = "http://Passport.NET/tb",
                    ServicePolicy = "LEGACY"
                }
            }
        });

        var responseModel = new CreateDeviceAccountResponseModel(
            new UserModel(response.Id, response.Cid, response.Puid, request.Username, $"{response.Puid:x2}@devices.relivewp.net"),
            [.. response.Tokens.Select(s =>
                new SecurityTokenModel(s.ServiceTarget, s.Token, s.TokenType, s.Created.ToDateTimeOffset(), s.Expires.ToDateTimeOffset()))]);

        return responseModel;
    }


    [Authorize]
    [ActionName("provision_device")]
    [HttpPost(Name = "provision_device")]
    public async Task<ActionResult<ProvisionDeviceResponseModel>> ProvisionDevice([FromBody] ProvisionDeviceRequestModel request)
    {
        // TODO: oh boy
        var puid = User.Claims.FirstOrDefault(c => c.Type == "puid")?.Value;
        var deviceCert = await authenticationClient.GetDeviceCertificateAsync(new DeviceCertificateRequest()
        {
            Puid = puid,
            CertificateRequest = ByteString.FromBase64(request.Csr)
        });

        return Ok(new ProvisionDeviceResponseModel(deviceCert.Certificate.ToBase64()));
    }
}
