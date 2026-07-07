
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Identity.Exchange;

internal class ExchangeAuthHandler(
    ILoggerFactory loggerFactory,
    ILogger<ExchangeAuthHandler> logger,
    IOptionsMonitor<ExchangeAuthOptions> options,
    UrlEncoder encoder,
    GrpcClientFactory grpcClientFactory) : AuthenticationHandler<ExchangeAuthOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "Exchange";
    private readonly Authentication.AuthenticationClient authenticationClient
        = grpcClientFactory.CreateClient<Authentication.AuthenticationClient>("Identity_GrpcClient");

    public new ExchangeAuthEvents Events
    {
        get => (ExchangeAuthEvents)base.Events!;
        set => base.Events = value;
    }

    /// <inheritdoc />
    protected override Task<object> CreateEventsAsync()
        => Task.FromResult<object>(new ExchangeAuthEvents());

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderNameOverride ?? "Authorization", out var value) ||
            string.IsNullOrWhiteSpace(value))
        {
            logger.LogInformation("No Authorization header found!");
            return AuthenticateResult.NoResult();
        }

        if (!AuthenticationHeaderValue.TryParse(value, out var header) || header.Scheme.ToLowerInvariant() != "basic")
        {
            logger.LogInformation("Invalid Authorization header specified!");
            return AuthenticateResult.NoResult();
        }

        var data = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter!));
        var username = data.Substring(0, data.IndexOf(':'));
        var password = data.Substring(data.IndexOf(':') + 1);

        VerifyResponse response;
        try
        {
            response = await authenticationClient.VerifyPasswordAsync(new VerifyPasswordRequest() {  EmailAddress = username, Password = password });
        }
        catch (RpcException ex)
        {
            logger.LogError(ex, "Failed to validate username/password combo!");
            return AuthenticateResult.Fail(ex);
        }

        if (response.Code > 0)
        {
            logger.LogError("Failed to validate with code {ErrorCode:X2}!", response.Code);
            return AuthenticateResult.Fail("Invalid token");
        }

        var claims = response.Claims.Select(c => new Claim(c.Type, c.Value));
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(principal, SchemeName);
        return AuthenticateResult.Success(ticket);
    }


    /// <inheritdoc />
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var authResult = await HandleAuthenticateOnceSafeAsync();
        var eventContext = new ExchangeChallengeContext(Context, Scheme, Options, properties)
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
        var forbiddenContext = new ExchangeForbiddenContext(Context, Scheme, Options);
        Response.StatusCode = 403;
        return Events.Forbidden(forbiddenContext);
    }
}