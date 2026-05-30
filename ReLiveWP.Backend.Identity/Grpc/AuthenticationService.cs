using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using ReLiveWP.Backend.Identity.Certificates;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Backend.Identity.Services;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Identity.Grpc;

public class AuthenticationService(
    TokenManager tokenManager,
    LiveIdDeviceCertificateService deviceCertificateService,
    UserManager<LiveUser> userManager) : Authentication.AuthenticationBase
{
    private const uint S_OK = 0x0;
    private const uint PPCRL_REQUEST_E_BAD_MEMBER_NAME_OR_PASSWORD = 0x80048821;
    private const uint PPCRL_AUTHSTATE_E_UNAUTHENTICATED = 0x80048800;
    private const uint PPCRL_AUTHSTATE_E_EXPIRED = 0x80048801;
    private const uint PPCRL_E_SQM_INTERNET_SEC_INVALID_CERT = 0x80048428;
    private const uint ERROR_ALREADY_EXISTS = 0x800700B7;

    public override async Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        if (await userManager.FindByNameAsync(request.Username) != null)
            return new RegisterResponse() { Code = ERROR_ALREADY_EXISTS };

        var (userId, cid, puid) = UserUtils.GenerateUserIds(LiveUserType.User);

        var user = new LiveUser()
        {
            Id = userId,
            Cid = cid,
            Puid = puid,
            UserName = request.Username,
            Email = request.EmailAddress,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, string.Join(", ", result.Errors.Select(s => s.Description))));
        }

        return new RegisterResponse() { Code = S_OK, Id = user.Id.ToString(), Cid = cid, Puid = puid };
    }

    public override async Task<VerifyTokenResponse> VerifySecurityToken(VerifyTokenRequest request, ServerCallContext context)
    {
        var result = await tokenManager.ValidateJwtAsync(request.Token, [.. request.ServiceTargets]);
        if (!result.IsValid)
        {
            // TODO: figure out what was actually invalid
            return new VerifyTokenResponse() { Code = PPCRL_AUTHSTATE_E_EXPIRED };
        }

        var response = new VerifyTokenResponse() { Code = S_OK };
        foreach (var claim in result.Claims)
        {
            if (claim.Value is string value) // TODO: other types
                response.Claims.Add(new ClaimMessage() { Type = claim.Key, Value = value });
        }

        return response;
    }

    public override async Task<SecurityTokensResponse> GetSecurityTokens(SecurityTokensRequest request, ServerCallContext context)
    {
        var user = await tokenManager.GetUserForSecurityTokenAsync(request);
        if (user == null)
        {
            return new SecurityTokensResponse() { Code = PPCRL_REQUEST_E_BAD_MEMBER_NAME_OR_PASSWORD };
        }

        var response = new SecurityTokensResponse()
        {
            Code = S_OK,
            Cid = user.Cid,
            Puid = user.Puid,
            Username = user.UserName,
            EmailAddress = user.Email
        };

        foreach (var tokenRequest in request.Requests)
        {
            // TODO: there's a few different tokens that can be requested here inclduing x509 certificates, for now
            // we're just working with JWTs which are our stand in for a BinarySecurityToken (aka a blob)
            var (token, created, expires) = await tokenManager.CreateJwtSecurityToken(user, tokenRequest.ServiceTarget);

            response.Tokens.Add(new SecurityTokenResponse()
            {
                ServiceTarget = tokenRequest.ServiceTarget,
                Created = Timestamp.FromDateTimeOffset(created),
                Expires = Timestamp.FromDateTimeOffset(expires),
                Token = token,
                TokenType = "JWT",
            });
        }

        return response;
    }

    public override async Task<RegisterDeviceResponse> RegisterDevice(RegisterDeviceRequest request, ServerCallContext context)
    {
        if (await userManager.FindByNameAsync(request.Username) != null)
            return new RegisterDeviceResponse() { Code = ERROR_ALREADY_EXISTS };

        var (userId, cid, puid) = UserUtils.GenerateUserIds(LiveUserType.Device);

        var user = new LiveUser()
        {
            Id = userId,
            Cid = cid,
            Puid = puid,
            UserName = request.Username,
            Email = $"{puid}@devices.relivewp.net",
            Type = LiveUserType.Device
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, string.Join(", ", result.Errors.Select(s => s.Description))));
        }

        var response = new RegisterDeviceResponse()
        {
            Code = S_OK,
            Id = user.Id.ToString(),
            Cid = cid,
            Puid = puid,
        }; 
        
        foreach (var tokenRequest in request.Requests)
        {
            // TODO: there's a few different tokens that can be requested here inclduing x509 certificates, for now
            // we're just working with JWTs which are our stand in for a BinarySecurityToken (aka a blob)
            var (token, created, expires) = await tokenManager.CreateJwtSecurityToken(user, tokenRequest.ServiceTarget);

            response.Tokens.Add(new SecurityTokenResponse()
            {
                ServiceTarget = tokenRequest.ServiceTarget,
                Created = Timestamp.FromDateTimeOffset(created),
                Expires = Timestamp.FromDateTimeOffset(expires),
                Token = token,
                TokenType = "JWT",
            });
        }

        return response;
    }

    public override Task<DeviceCertificateResponse> GetDeviceCertificate(DeviceCertificateRequest request, ServerCallContext context)
    {
        var cert = deviceCertificateService.HandleCertRequest(request.Puid, request.CertificateRequest.ToByteArray());
        return Task.FromResult(new DeviceCertificateResponse() { Succeeded = true, Certificate = ByteString.CopyFrom(cert) });
    }
}