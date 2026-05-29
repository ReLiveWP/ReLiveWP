
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Grpc.Net.ClientFactory;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Identity.LiveID;

internal class LiveIDAuthHandler : AuthenticationHandler<LiveIDAuthOptions>
{
    public const string SchemeName = "LiveID";
    private readonly Authentication.AuthenticationClient authenticationClient;
    private readonly ILogger<LiveIDAuthHandler> logger;

    public LiveIDAuthHandler(
        ILoggerFactory loggerFactory,
        ILogger<LiveIDAuthHandler> logger,
        GrpcClientFactory grpcClientFactory,
        IOptionsMonitor<LiveIDAuthOptions> options,
        UrlEncoder encoder) : base(options, loggerFactory, encoder)
    {
        this.logger = logger;
        this.authenticationClient = grpcClientFactory.CreateClient<Authentication.AuthenticationClient>("Identity_GrpcClient");
    }

    public new LiveIDAuthEvents Events
    {
        get => (LiveIDAuthEvents)base.Events!;
        set => base.Events = value;
    }

    /// <inheritdoc />
    protected override Task<object> CreateEventsAsync()
        => Task.FromResult<object>(new LiveIDAuthEvents());

    protected override async Task InitializeHandlerAsync()
    {
        await base.InitializeHandlerAsync();

        if (!Options.ValidServiceTargets.Any())
        {
            throw new Exception("No valid service targets specified!");
        }
    }

    /// <summary>
    /// Searches the 'Authorization' header for a 'Bearer' token. If the 'Bearer' token is found, it is validated via the gRPC authentication service.
    /// </summary>
    /// <returns></returns>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderNameOverride ?? "Authorization", out var value))
        {
            logger.LogInformation("No authorization header found!");
            return AuthenticateResult.NoResult();
        }

        var authHeader = value.ToString();
        string token;

        if (Options.AcceptBasicAuth && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            // EAS Basic auth: Authorization: Basic base64(username:access_token)
            // The password field carries the access token; username is ignored for verification.
            var encoded = authHeader["Basic ".Length..].Trim();
            string decoded;
            try { decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded)); }
            catch { return AuthenticateResult.Fail("Malformed Basic auth header"); }

            var colon = decoded.IndexOf(':');
            if (colon < 0)
                return AuthenticateResult.Fail("Malformed Basic auth header: missing colon");

            token = decoded[(colon + 1)..];
        }
        else if (authHeader.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase))
            token = authHeader["Bearer".Length..].Trim();
        else if (authHeader.StartsWith("WLID1.0", StringComparison.OrdinalIgnoreCase))
            token = authHeader["WLID1.0".Length..].Trim();
        else
            token = authHeader;

        var request = new VerifyTokenRequest { Token = token, TokenType = "JWT" };
        foreach (var validService in Options.ValidServiceTargets)
        {
            request.ServiceTargets.Add(validService);
        }

        VerifyTokenResponse reply;
        try
        {
            reply = await authenticationClient.VerifySecurityTokenAsync(request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate token!");
            return AuthenticateResult.Fail($"Auth service error: {ex.Message}");
        }

        if (reply.Code > 0)
        {
            logger.LogError("Failed to validate with code {ErrorCode:X2}!", reply.Code);
            return AuthenticateResult.Fail("Invalid token");
        }

        var claims = reply.Claims.Select(c => new Claim(c.Type, c.Value));
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(principal, SchemeName);
        return AuthenticateResult.Success(ticket);
    }


    /// <inheritdoc />
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var authResult = await HandleAuthenticateOnceSafeAsync();
        var eventContext = new LiveIDChallengeContext(Context, Scheme, Options, properties)
        {
            AuthenticateFailure = authResult?.Failure
        };

        await Events.Challenge(eventContext);
        if (eventContext.Handled)
        {
            return;
        }

        Response.StatusCode = 401;
        if (Options.AcceptBasicAuth)
            Response.Headers.WWWAuthenticate = $"Basic realm=\"{Options.BasicAuthRealm}\"";
        await Response.WriteAsync("Unauthorized");
    }

    /// <inheritdoc />
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        var forbiddenContext = new LiveIDForbiddenContext(Context, Scheme, Options);
        Response.StatusCode = 403;
        return Events.Forbidden(forbiddenContext);
    }
}