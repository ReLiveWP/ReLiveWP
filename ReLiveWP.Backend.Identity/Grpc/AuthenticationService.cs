using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Identity.Certificates;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Backend.Identity.Services;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Identity.Grpc;

public class AuthenticationService(
    TokenManager tokenManager,
    LiveIdDeviceCertificateService deviceCertificateService,
    UserManager<LiveUser> userManager,
    LiveDbContext dbContext,
    ILogger<AuthenticationService> logger) : Authentication.AuthenticationBase
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
            Email = $"{(ulong)puid:x2}@devices.relivewp.net",
            Type = LiveUserType.Device,
            DeviceId = request.DeviceId
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, string.Join(", ", result.Errors.Select(s => s.Description))));

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

    public override Task<DeviceCertificateResponse> GetDeviceCertificate(DeviceCertificateRequest request, ServerCallContext context)
    {
        var cert = deviceCertificateService.HandleCertRequest(request.Puid, request.CertificateRequest.ToByteArray());
        return Task.FromResult(new DeviceCertificateResponse() { Succeeded = true, Certificate = ByteString.CopyFrom(cert) });
    }

    public override async Task<ValidateDeviceCertificateResponse> ValidateDeviceCertificate(ValidateDeviceCertificateRequest request, ServerCallContext context)
    {
        var cert = X509CertificateLoader.LoadCertificate(request.Certificate.ToByteArray());
        if (!deviceCertificateService.ValidateDeviceCertificate(cert))
            return new ValidateDeviceCertificateResponse() { Succeeded = false };

        var cn = cert.Subject
                     .Split(',')
                     .Select(part => part.Trim())
                     .First(part => part.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                     .Substring(3);

        if (!cn.EndsWith("devicedns.live.com"))
            return new ValidateDeviceCertificateResponse() { Succeeded = false };

        var puid = long.Parse(cn.Split('.')[0], NumberStyles.HexNumber);
        if (!UserUtils.IsValidPuid(LiveUserType.Device, (ulong)puid))
            logger.LogWarning("Certificate with an invalid PUID, likely an old user account. Client may need updating!");

        var user = await dbContext.Users.FirstOrDefaultAsync(f => f.Puid == puid);

        if (user == null)
        {
            // the certificate validates fine, we just dont really know who this is, this is kinda a nasty fallback 
            // but it's honestly fine so long as the Ids remain consistent.
            return new ValidateDeviceCertificateResponse() { Succeeded = true, DeviceId = cn };
        }

        return new ValidateDeviceCertificateResponse()
        {
            Succeeded = true,
            Cid = user.Cid,
            Puid = user.Puid,
            EmailAddress = user.Email,
            Username = user.UserName,
            DeviceId = cn
        };
    }
}