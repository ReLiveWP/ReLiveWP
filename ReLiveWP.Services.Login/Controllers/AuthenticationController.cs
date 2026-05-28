using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
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
    ConnectedServices.ConnectedServicesClient connectedServicesClient,
    DeviceRegistration.DeviceRegistrationClient deviceRegistrationClient,
    ClientProvisioning.ClientProvisioningClient clientProvisioningClient) : ControllerBase
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

        return new UserModel(User.Id()!, user.Cid, user.Puid, user.Username, user.EmailAddress);
    }

    [Authorize]
    [Route("/auth/user/@me/linked-accounts")]
    public async Task<ActionResult<ConnectionModels>> GetLinkedAccounts()
    {
        var auth = Request.Headers.Authorization.ToString();
        var authHeader = string.Concat("Bearer ", auth.AsSpan(auth.IndexOf(' ')));
        var connections = connectedServicesClient.GetConnections(new ConnectionsRequest(), new Metadata() { { "Authorization", authHeader } });
        if (connections == null)
            return NotFound(); // this is pretty bad, maybe 500 is better?

        var connectionModels = new Dictionary<string, List<ConnectionModel>>();
        await foreach (var connection in connections.ResponseStream.ReadAllAsync())
        {
            if (!connectionModels.TryGetValue(connection.Service, out var connectionList))
            {
                connectionModels[connection.Service] = connectionList = [];
            }

            connectionList.Add(new ConnectionModel(connection.Id, connection.ServiceUrl, connection.UserName, (connection.Flags & 0x80000000UL) != 0));
        }

        return new ConnectionModels(connectionModels);
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
        try
        {
            // TODO oh boy howdy this needs to go away
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
            Marshal.ThrowExceptionForHR((int)grpcResponse.Code); // TODO: fix all of this please god

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
        catch (Exception ex)
        {
            return Unauthorized(new ErrorModel((uint)ex.HResult));
        }
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
            new UserModel(response.Id, response.Cid, response.Puid, request.Username, $"{response.Puid}@devices.relivewp.net"),
            [.. response.Tokens.Select(s =>
                new SecurityTokenModel(s.ServiceTarget, s.Token, s.TokenType, s.Created.ToDateTimeOffset(), s.Expires.ToDateTimeOffset()))]);

        return responseModel;
    }


    [Authorize]
    [ActionName("provision_device")]
    [HttpPost(Name = "provision_device")]
    public async Task<ActionResult<ProvisionDeviceResponseModel>> ProvisionDevice([FromBody] ProvisionDeviceRequestModel request)
    {
        var puid = User.Claims.FirstOrDefault(c => c.Type == "puid")?.Value;
        var deviceCert = await authenticationClient.GetDeviceCertificateAsync(new DeviceCertificateRequest()
        {
            Puid = puid,
            CertificateRequest = ByteString.FromBase64(request.Csr)
        });

        return Ok(new ProvisionDeviceResponseModel(deviceCert.Certificate.ToBase64()));
    }
}
