using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Backend.Identity.Services;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Identity.Grpc;

public class SsoService(
    SsoSessionManager sessions,
    TokenManager tokenManager,
    UserManager<LiveUser> userManager,
    ILogger<SsoService> logger) : Sso.SsoBase
{
    private const uint S_OK = 0x0;
    private const uint PPCRL_AUTHSTATE_E_UNAUTHENTICATED = 0x80048800;
    private const uint HTTP_E_STATUS_BAD_REQUEST = 0x80190190;

    // a real PBKDF2 hash of a value nobody knows, so the no-such-user path does the same work
    private const string DummyPasswordHash =
        "AQAAAAIAAYagAAAAEIBcVYIEKgYyt4Xn/6cIx0kJcAtVwWiPKlBs0Xw7qX5wOTKgxKQhZbxRPtnkVtLmXA==";

    /// <summary>
    /// The web sign-in form is for real accounts only. Device accounts also carry a username and
    /// password (RegisterDevice sets both), and must not be able to establish a browser session.
    /// </summary>
    private async Task<LiveUser?> FindWebUserAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        var user = await userManager.FindByEmailAsync(username) ?? await userManager.FindByNameAsync(username);
        return user?.Type == LiveUserType.User ? user : null;
    }

    public override async Task<CreateSessionResponse> SignIn(SsoSignInRequest request, ServerCallContext context)
    {
        var user = await FindWebUserAsync(request.Username);

        // verify against a throwaway hash when there is no user, so a missing account and a wrong
        // password cost the same and neither is distinguishable by timing
        if (user == null)
        {
            await Task.Run(() => userManager.PasswordHasher.VerifyHashedPassword(
                new LiveUser(), DummyPasswordHash, request.Password));
            return new CreateSessionResponse() { Code = PPCRL_AUTHSTATE_E_UNAUTHENTICATED };
        }

        // checked before the verify, or a locked account can still be probed indefinitely
        if (await userManager.IsLockedOutAsync(user))
            return new CreateSessionResponse() { Code = PPCRL_AUTHSTATE_E_UNAUTHENTICATED };

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return new CreateSessionResponse() { Code = PPCRL_AUTHSTATE_E_UNAUTHENTICATED };
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var created = await sessions.CreateSessionAsync(
            user.Id,
            request.Persistent,
            request.HasUserAgent ? request.UserAgent : null,
            request.HasCreatedIp ? request.CreatedIp : null);

        return new CreateSessionResponse()
        {
            Code = S_OK,
            SessionHandle = created.Handle,
            SessionId = created.SessionId.ToString(),
            Expires = Timestamp.FromDateTimeOffset(created.Expires),
        };
    }

    public override async Task<ValidateSessionResponse> ValidateSession(ValidateSessionRequest request, ServerCallContext context)
    {
        var session = await sessions.ValidateSessionAsync(request.SessionHandle);
        if (session == null)
            return new ValidateSessionResponse() { Valid = false };

        var user = await userManager.FindByIdAsync(session.UserId.ToString());
        if (user == null)
            return new ValidateSessionResponse() { Valid = false };

        return new ValidateSessionResponse()
        {
            Valid = true,
            UserId = session.UserId.ToString(),
            SessionId = session.Id.ToString(),
            Username = user.UserName,
            Expires = Timestamp.FromDateTimeOffset(session.ExpiresAt),
        };
    }

    public override async Task<RevokeSessionResponse> RevokeSession(RevokeSessionRequest request, ServerCallContext context)
        => new() { Revoked = await sessions.RevokeSessionAsync(request.SessionHandle) };

    public override async Task<ListSessionsResponse> ListSessions(ListSessionsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
            return new ListSessionsResponse();

        var response = new ListSessionsResponse();
        foreach (var session in await sessions.ListSessionsAsync(userId))
        {
            var info = new SessionInfo()
            {
                Id = session.Id.ToString(),
                CreatedAt = Timestamp.FromDateTimeOffset(session.CreatedAt),
                LastSeenAt = Timestamp.FromDateTimeOffset(session.LastSeenAt),
                ExpiresAt = Timestamp.FromDateTimeOffset(session.ExpiresAt),
            };

            if (session.UserAgent != null) info.UserAgent = session.UserAgent;
            if (session.CreatedIp != null) info.CreatedIp = session.CreatedIp;

            response.Sessions.Add(info);
        }

        return response;
    }

    public override async Task<IssueAuthorizationCodeResponse> IssueAuthorizationCode(
        IssueAuthorizationCodeRequest request, ServerCallContext context)
    {
        var session = await sessions.ValidateSessionAsync(request.SessionHandle);
        if (session == null)
            return new IssueAuthorizationCodeResponse() { Code = PPCRL_AUTHSTATE_E_UNAUTHENTICATED };

        if (request.ServiceTargets.Count == 0 || string.IsNullOrWhiteSpace(request.RedirectUri))
            return new IssueAuthorizationCodeResponse() { Code = HTTP_E_STATUS_BAD_REQUEST };

        var code = await sessions.IssueAuthorizationCodeAsync(new SsoAuthorizationCodePayload(
            session.UserId,
            session.Id,
            request.ClientId,
            request.RedirectUri,
            [.. request.ServiceTargets],
            request.HasCodeChallenge ? request.CodeChallenge : null,
            request.HasCodeChallengeMethod ? request.CodeChallengeMethod : null));

        return new IssueAuthorizationCodeResponse()
        {
            Code = S_OK,
            AuthorizationCode = code,
            Persistent = session.Persistent,
        };
    }

    public override async Task<SecurityTokensResponse> RedeemAuthorizationCode(
        RedeemAuthorizationCodeRequest request, ServerCallContext context)
    {
        var payload = await sessions.RedeemAuthorizationCodeAsync(
            request.AuthorizationCode,
            request.ClientId,
            request.RedirectUri,
            request.HasCodeVerifier ? request.CodeVerifier : null);

        if (payload == null)
            return new SecurityTokensResponse() { Code = PPCRL_AUTHSTATE_E_UNAUTHENTICATED };

        var user = await userManager.FindByIdAsync(payload.UserId.ToString());
        if (user == null)
        {
            logger.LogWarning("Authorization code redeemed for missing user {User}", payload.UserId);
            return new SecurityTokensResponse() { Code = PPCRL_AUTHSTATE_E_UNAUTHENTICATED };
        }

        var response = new SecurityTokensResponse()
        {
            Code = S_OK,
            Id = user.Id.ToString(),
            Cid = user.Cid,
            Puid = user.Puid,
            Username = user.UserName,
            EmailAddress = user.Email,
        };

        foreach (var target in payload.ServiceTargets)
        {
            var (token, created, expires) = tokenManager.IssueJwtAsync(
                user, target, tokenManager.WebAccessTokenLifetime, payload.SessionId);
            var refresh = await tokenManager.IssueRefreshTokenAsync(
                user, target, tokenManager.WebRefreshTokenLifetime, payload.SessionId);

            response.Tokens.Add(new SecurityTokenResponse()
            {
                ServiceTarget = target,
                Token = token,
                TokenType = "JWT",
                Created = Timestamp.FromDateTimeOffset(created),
                Expires = Timestamp.FromDateTimeOffset(expires),
                RefreshToken = refresh.Token,
                RefreshTokenExpires = Timestamp.FromDateTimeOffset(refresh.Expires),
                Code = S_OK,
            });
        }

        return response;
    }
}
