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

record class ErrorModel(uint ErrorCode);
public record CreateAccountModel(string Username, string Password, string EmailAddress);
public record UserModel(string Id, string Cid, string Puid, string Username, string EmailAddress);
public record UserIdentityModel(string Id, string Cid, long Puid, string Username, string Password);
public record ProvisionDeviceRequestModel(string DeviceId, string Csr);
public record ProvisionDeviceResponseModel(UserIdentityModel Identity, SecurityTokenModel[] SecurityTokens, string DeviceCert);
public record ConnectionModel(string Id, string Url, string Name);
public record ConnectionModels(Dictionary<string, List<ConnectionModel>> Connections);

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

        return new UserModel(User.Id()!, user.Cid, user.Puid.ToString(), user.Username, user.EmailAddress);
    }

    [Authorize]
    [Route("/auth/user/@me/linked-accounts")]
    public async Task<ActionResult<ConnectionModels>> GetLinkedAccounts()
    {
        var auth = Request.Headers.Authorization.ToString();
        var authHeader = string.Concat("Bearer ", auth.AsSpan(auth.IndexOf(' ')));
        var connections = await connectedServicesClient.GetConnectionsAsync(new ConnectionsRequest(), new Metadata() { { "Authorization", authHeader } });
        if (connections == null)
            return NotFound(); // this is pretty bad, maybe 500 is better?

        var connectionModels = new Dictionary<string, List<ConnectionModel>>();
        foreach (var connection in connections.Connections)
        {
            if (!connectionModels.TryGetValue(connection.Service, out var connectionList))
            {
                connectionModels[connection.Service] = connectionList = [];
            }

            connectionList.Add(new ConnectionModel(connection.Id, connection.ServiceUrl, connection.UserName));
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

    [Authorize]
    [ActionName("provision_device")]
    [HttpPost(Name = "provision_device")]
    public async Task<ActionResult<ProvisionDeviceResponseModel>> ProvisionDevice([FromBody] ProvisionDeviceRequestModel request)
    {
        try
        {
            var userName = request.DeviceId + "_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
            var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            var account = await authenticationClient.RegisterAsync(new RegisterRequest() { Username = userName, EmailAddress = "", Password = password });
            Marshal.ThrowExceptionForHR((int)account.Code); // TODO: fix all of this please god

            var associationRequest = new DeviceAssociationRequest() { DeviceId = request.DeviceId, UserId = User.Id() };
            var associationResponse = await deviceRegistrationClient.AssociateDeviceWithUserAsync(associationRequest);
            if (!associationResponse.Succeeded)
            {
                // weird but not the end of the world tbh
            }

            var deviceCert = await clientProvisioningClient.ProvisionWP7DeviceAsync(new WP7ProvisioningRequest() { CertificateRequest = ByteString.FromBase64(request.Csr) });
            if (!deviceCert.Succeeded)
                throw new Exception("Failed to provision certificate");

            var certificateCollection = new X509Certificate2Collection();
            certificateCollection.Import(deviceCert.Certificate.ToByteArray());
            var certificate = certificateCollection.First()!;
            var encodedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

            var grpcRequest = new SecurityTokensRequest()
            {
                Username = userName,
                Password = password,
            };

            var grpcTokenRequest = new SecurityTokenRequest()
            {
                ServicePolicy = "JWT",
                ServiceTarget = "http://Passport.NET/tb"
            };

            grpcRequest.Requests.Add(grpcTokenRequest);

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

            var response = new ProvisionDeviceResponseModel(
                new UserIdentityModel(userName, account.Cid, account.Puid, userName, password),
                [.. securityTokens],
                encodedCertificate);

            return response;
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorModel((uint)ex.HResult));
        }
    }
}
