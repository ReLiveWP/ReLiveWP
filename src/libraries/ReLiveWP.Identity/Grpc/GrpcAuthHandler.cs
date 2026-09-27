
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ReLiveWP.Identity.Grpc;

internal class GrpcAuthHandler(
    ILoggerFactory loggerFactory,
    ILogger<GrpcAuthHandler> logger,
    IOptionsMonitor<GrpcAuthOptions> options,
    UrlEncoder encoder) : AuthenticationHandler<GrpcAuthOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "Grpc";

    public new GrpcAuthEvents Events
    {
        get => (GrpcAuthEvents)base.Events!;
        set => base.Events = value;
    }

    /// <inheritdoc />
    protected override Task<object> CreateEventsAsync()
        => Task.FromResult<object>(new GrpcAuthEvents());

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderNameOverride ?? "X-User-Id", out var value) ||
            string.IsNullOrWhiteSpace(value))
        {
            logger.LogInformation("No X-User-ID header found!");
            return AuthenticateResult.NoResult();
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, value!)], SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return AuthenticateResult.Success(ticket);
    }


    /// <inheritdoc />
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var authResult = await HandleAuthenticateOnceSafeAsync();
        var eventContext = new GrpcChallengeContext(Context, Scheme, Options, properties)
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
        var forbiddenContext = new GrpcForbiddenContext(Context, Scheme, Options);
        Response.StatusCode = 403;
        return Events.Forbidden(forbiddenContext);
    }
}