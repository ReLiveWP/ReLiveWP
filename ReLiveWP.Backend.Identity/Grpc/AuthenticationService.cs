using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
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
    private const string LegacyDaTokenTarget = "http://Passport.NET/tb";

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
            var tokenResponse = await IssueTokenAsync(user, tokenRequest);
            if (tokenResponse == null)
                continue;

            response.Tokens.Add(tokenResponse);
        }

        return response;
    }

    public override async Task<VerifyResponse> VerifySecurityToken(VerifyTokenRequest request, ServerCallContext context)
    {
        var result = await tokenManager.ValidateJwtAsync(request.Token, [.. request.ServiceTargets]);

        if (!result.IsValid)
        {
            return new VerifyResponse() { Code = PPCRL_AUTHSTATE_E_EXPIRED };
        }

        var response = new VerifyResponse() { Code = S_OK, Id = result.ClaimsIdentity.Claims.First(c => c.Type == ClaimTypes.NameIdentifier)!.Value };
        foreach (var claim in result.Claims)
        {
            if (claim.Value is string value) // TODO: other types
                response.Claims.Add(new ClaimMessage() { Type = claim.Key, Value = value });
        }

        return response;
    }

    public override async Task<SecurityTokensResponse> GetSecurityTokens(SecurityTokensRequest request, ServerCallContext context)
    {
        var authenticated = await tokenManager.GetUserForSecurityTokenAsync(request);
        if (authenticated == null)
        {
            return new SecurityTokensResponse() { Code = PPCRL_REQUEST_E_BAD_MEMBER_NAME_OR_PASSWORD };
        }

        var user = authenticated.User;
        var response = new SecurityTokensResponse()
        {
            Code = S_OK,
            Id = user.Id.ToString(),
            Cid = user.Cid,
            Puid = user.Puid,
            Username = user.UserName,
            EmailAddress = user.Email,
        };

        if (authenticated.SessionKey is { } sk)
        {
            response.SessionKey = ByteString.CopyFrom(sk);
        }

        foreach (var tokenRequest in request.Requests)
        {
            var tokenResponse = await IssueTokenAsync(user, tokenRequest, request.IssueRefreshToken);
            if (tokenResponse == null)
                continue;

            response.Tokens.Add(tokenResponse);
        }

        return response;
    }

    private async Task<SecurityTokenResponse?> IssueTokenAsync(LiveUser user, SecurityTokenRequest tokenRequest, bool issueRefreshToken = false)
    {
        if (tokenRequest.ServiceTarget == LegacyDaTokenTarget)
        {
            var da = tokenManager.IssueDeviceAuthToken(user);

            return new()
            {
                ServiceTarget = tokenRequest.ServiceTarget,
                Created = Timestamp.FromDateTimeOffset(da.Created),
                Expires = Timestamp.FromDateTimeOffset(da.Expires),
                Token = da.SealedToken,
                TokenType = "urn:passport:legacy",
                ProofKey = ByteString.CopyFrom(da.ProofKey),
            };
        }

        if (tokenRequest.ServicePolicy == "MBI_X509_DID")
        {
            // device cert time :D
            var (thumb, cert, certExpiry) = deviceCertificateService.IssueCertificateAsync(user.Puid.ToString("x16"), [.. tokenRequest.SupportingData]);
            user.Certificates.Add(new LiveUserCertificate()
            {
                UserId = user.Id,
                Fingerprint = thumb
            });

            dbContext.Update(user);
            await dbContext.SaveChangesAsync();

            return new()
            {
                ServiceTarget = tokenRequest.ServiceTarget,
                Created = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
                Expires = Timestamp.FromDateTimeOffset(certExpiry),
                Token = Convert.ToBase64String(cert),
                TokenType = tokenRequest.ServicePolicy
            };
        }

        var (token, created, expires) = tokenManager.IssueJwtAsync(user, tokenRequest.ServiceTarget);
        var tokenResponse = new SecurityTokenResponse()
        {
            ServiceTarget = tokenRequest.ServiceTarget,
            Created = Timestamp.FromDateTimeOffset(created),
            Expires = Timestamp.FromDateTimeOffset(expires),
            Token = token,
            TokenType = tokenRequest.ServicePolicy
        };

        if (tokenRequest.ServicePolicy.EndsWith("_KEY"))
            tokenResponse.ProofKey = ByteString.CopyFrom(RandomNumberGenerator.GetBytes(24));

        if (issueRefreshToken)
        {
            var refresh = await tokenManager.IssueRefreshTokenAsync(user, tokenRequest.ServiceTarget);
            tokenResponse.RefreshToken = refresh.Token;
            tokenResponse.RefreshTokenExpires = Timestamp.FromDateTimeOffset(refresh.Expires);
        }

        return tokenResponse;
    }

    public override async Task<SecurityTokensResponse> RefreshSecurityTokens(RefreshTokensRequest request, ServerCallContext context)
    {
        if (request.RefreshTokens.Count == 0)
            return new SecurityTokensResponse() { Code = 0x80190190 }; // HTTP_E_STATUS_BAD_REQUEST

        var response = new SecurityTokensResponse() { Code = S_OK };

        LiveUser? user = null;
        foreach (var refreshToken in request.RefreshTokens)
        {
            var redemption = await tokenManager.RedeemRefreshTokenAsync(refreshToken);
            if (redemption == null)
            {
                response.Tokens.Add(new SecurityTokenResponse() { Code = PPCRL_AUTHSTATE_E_EXPIRED });
                continue;
            }

            user ??= redemption.User;

            var (token, created, expires) = tokenManager.IssueJwtAsync(redemption.User, redemption.ServiceTarget);
            var refresh = await tokenManager.IssueRefreshTokenAsync(redemption.User, redemption.ServiceTarget);

            response.Tokens.Add(new SecurityTokenResponse()
            {
                ServiceTarget = redemption.ServiceTarget,
                Created = Timestamp.FromDateTimeOffset(created),
                Expires = Timestamp.FromDateTimeOffset(expires),
                Token = token,
                TokenType = "JWT",
                RefreshToken = refresh.Token,
                RefreshTokenExpires = Timestamp.FromDateTimeOffset(refresh.Expires),
                Code = S_OK,
            });
        }

        if (user == null)
            return new SecurityTokensResponse() { Code = 0x80190190 };

        response.Id = user.Id.ToString();
        response.Cid = user.Cid;
        response.Puid = user.Puid;
        response.Username = user.UserName;
        response.EmailAddress = user.Email;

        return response;
    }

    public override Task<DeviceCertificateResponse> GetDeviceCertificate(DeviceCertificateRequest request, ServerCallContext context)
    {
        var cert = deviceCertificateService.IssueCertificateAsync(request.Puid, request.CertificateRequest.ToByteArray());
        return Task.FromResult(new DeviceCertificateResponse() { Succeeded = true, Certificate = ByteString.CopyFrom(cert.Certificate) });
    }

    public override async Task<VerifyResponse> VerifyPassword(VerifyPasswordRequest request, ServerCallContext context)
    {
        var user =
            request.HasUsername ?
            await userManager.FindByNameAsync(request.Username) :
            await userManager.FindByEmailAsync(request.EmailAddress);

        if (user == null)
            return new VerifyResponse() { Code = PPCRL_REQUEST_E_BAD_MEMBER_NAME_OR_PASSWORD };

        if (!await userManager.CheckPasswordAsync(user, request.Password))
            return new VerifyResponse() { Code = PPCRL_REQUEST_E_BAD_MEMBER_NAME_OR_PASSWORD };

        var response = new VerifyResponse() { Code = S_OK, Id = user.Id.ToString() };
        foreach (var claim in tokenManager.BuildIdentityClaims(user))
        {
            // verifying a JWT maps some claims to standardised names, we're mostly just after the user's ID though 
            // so NameIdentifier all we map here
            if (claim.Type == JwtRegisteredClaimNames.Sub)
                response.Claims.Add(new ClaimMessage() { Type = ClaimTypes.NameIdentifier, Value = claim.Value });

            response.Claims.Add(new ClaimMessage() { Type = claim.Type, Value = claim.Value });
        }
        return response;
    }

    public override async Task<VerifyDeviceCertificateResponse> VerifyDeviceCertificate(VerifyDeviceCertificateRequest request, ServerCallContext context)
    {
        var cert = X509CertificateLoader.LoadCertificate(request.Certificate.ToByteArray());
        if (!deviceCertificateService.ValidateDeviceCertificate(cert))
            return new VerifyDeviceCertificateResponse() { Succeeded = false };

        LiveUser? user = null;

        var cn = cert.Subject
                     .Split(',')
                     .Select(part => part.Trim())
                     .First(part => part.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                     .Substring(3);

        if (!cn.EndsWith("devicedns.live.com"))
        {
            //return new ValidateDeviceCertificateResponse() { Succeeded = false };
            var thumbprint = cert.Thumbprint;
            user = dbContext.Users.FirstOrDefault(u => u.Certificates.Any(c => c.Fingerprint == thumbprint));
            if (user != null)
            {
                cn = $"{user.Puid.ToString("X16")}.devicedns.live.com";
            }
        }

        var puid = long.Parse(cn.Split('.')[0], NumberStyles.HexNumber);
        if (!UserUtils.IsValidPuid(LiveUserType.Device, (ulong)puid))
            logger.LogWarning("Certificate with an invalid PUID, likely an old user account. Client may need updating!");

        user = await dbContext.Users.FirstOrDefaultAsync(f => f.Puid == puid);

        if (user == null)
        {
            // the certificate validates fine, we just dont really know who this is, this is kinda a nasty fallback 
            // but it's honestly fine so long as the Ids remain consistent.
            return new VerifyDeviceCertificateResponse() { Succeeded = true, DeviceId = cn };
        }

        return new VerifyDeviceCertificateResponse()
        {
            Succeeded = true,
            Id = user.Id.ToString(),
            Cid = user.Cid,
            Puid = user.Puid,
            EmailAddress = user.Email,
            Username = user.UserName,
            DeviceId = cn
        };
    }

    public override async Task<Empty> ChangePassword(ChangePasswordRequest request, ServerCallContext context)
    {

        LiveUser? user = null;
        if (request.HasUid)
        {
            var uid = Guid.Parse(request.Uid);
            user = await dbContext.Users.FindAsync(uid);
        }
        else if (request.HasPuid)
        {
            var puid = long.Parse(request.Puid, NumberStyles.HexNumber);
            user = await dbContext.Users.FirstOrDefaultAsync(f => f.Puid == puid);
        }

        if (user == null)
            throw new RpcException(new Status(StatusCode.NotFound, "User not found!"));

        var result = await userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        if (!result.Succeeded)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid username or password."));

        return new Empty();
    }
}