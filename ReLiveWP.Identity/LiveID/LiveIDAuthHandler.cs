
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ReLiveWP.Identity.LiveID;

internal class LiveIDAuthHandler(
    ILoggerFactory loggerFactory,
    ILogger<LiveIDAuthHandler> logger,
    IOptionsMonitor<LiveIDAuthOptions> options,
    UrlEncoder encoder,
    IConfiguration configuration) : AuthenticationHandler<LiveIDAuthOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "LiveID";

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

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderNameOverride ?? "Authorization", out var value))
        {
            logger.LogInformation("No authorization header found!");
            return AuthenticateResult.NoResult();
        }

        string? token;
        if (AuthenticationHeaderValue.TryParse(value.ToString(), out var header) && !string.IsNullOrWhiteSpace(header?.Parameter))
            token = header.Parameter!;
        else
            token = value.ToString().Trim();

        if (string.IsNullOrWhiteSpace(token))
            return AuthenticateResult.Fail("Missing token");

        if (token.StartsWith("t="))
        {
            try
            {
                var query = HttpUtility.ParseQueryString(token);
                token = query["t"]!.Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse query string formatted WLID token!");
                return AuthenticateResult.Fail(ex);
            }
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtKeyLoader.GetVerifyingKey(configuration, logger),

            ValidateIssuer = true,
            ValidIssuer = JwtKeyLoader.Issuer,

            ValidateAudience = true,
            ValidAudiences = Options.ValidServiceTargets,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5) // +- 5 mins is fine, these devices are Old
        };

        TokenValidationResult result;
        try
        {
            result = await new JwtSecurityTokenHandler().ValidateTokenAsync(token, validationParameters);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate token!");
            return AuthenticateResult.Fail(ex);
        }

        if (!result.IsValid)
        {
            logger.LogWarning(result.Exception, "Invalid token");
            return AuthenticateResult.Fail(result.Exception ?? new Exception("Invalid token"));
        }

        var identity = new ClaimsIdentity(result.ClaimsIdentity.Claims, SchemeName);
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