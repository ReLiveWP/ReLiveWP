using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Login.Models;
using ReLiveWP.Services.Login.Models.Sso;
using ReLiveWP.Services.Login.Services;

namespace ReLiveWP.Services.Login.Controllers;

[AllowAnonymous]
[Route("sso")]
public class SsoController(
    Sso.SsoClient ssoClient,
    PendingAuthorizeStore pendingStore,
    IOptions<SsoOptions> options,
    ILogger<SsoController> logger) : Controller
{
    private const uint S_OK = 0x0;

    private SsoOptions Options => options.Value;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // the sign-in ceremony must never be framed, and the code must never ride a Referer out
        Response.Headers["X-Frame-Options"] = "DENY";
        Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers.CacheControl = "no-store";

        base.OnActionExecuting(context);
    }

    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize(
        string? client_id,
        string? redirect_uri,
        string? state,
        string? scope,
        string? code_challenge,
        string? code_challenge_method,
        string? prompt,
        CancellationToken cancellationToken)
    {
        var client = Options.FindClient(client_id);
        if (client == null || !client.IsRedirectUriAllowed(redirect_uri))
            return InvalidRequest("Unknown application", "That sign-in request did not come from an application we recognise.");

        // past this point redirect_uri is validated, so protocol errors may go back to it
        if (string.IsNullOrWhiteSpace(state))
            return RedirectWithError(redirect_uri!, null, "invalid_request", "state is required");

        var targets = ParseScope(scope);
        if (targets.Length == 0 || !client.AreTargetsAllowed(targets))
            return RedirectWithError(redirect_uri!, state, "invalid_scope", "requested scope is not allowed for this application");

        if (client.RequirePkce && string.IsNullOrWhiteSpace(code_challenge))
            return RedirectWithError(redirect_uri!, state, "invalid_request", "code_challenge is required");

        if (code_challenge is { Length: > 0 } && code_challenge_method is not (null or "S256" or "plain"))
            return RedirectWithError(redirect_uri!, state, "invalid_request", "unsupported code_challenge_method");

        var pending = new PendingAuthorize(
            client_id!, redirect_uri!, state, targets, code_challenge, code_challenge_method ?? "S256");

        var handle = Request.Cookies[Options.Cookie.Name];
        if (handle is { Length: > 0 })
        {
            var code = await IssueCodeAsync(handle, pending, cancellationToken);
            if (code != null)
                return RedirectWithCode(redirect_uri!, state, code);
        }

        if (prompt == "none")
            return RedirectWithError(redirect_uri!, state, "login_required", "no active session");

        return View("SignIn", new SignInViewModel
        {
            PendingId = await pendingStore.CreateAsync(pending),
            RememberMe = true,
        });
    }

    [HttpPost("signin")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("SsoSignIn")]
    public async Task<IActionResult> SignIn(SignInViewModel model, CancellationToken cancellationToken)
    {
        // everything that decides where the user ends up comes from the parked request, never
        // from the form. the form only carries which request it belongs to.
        var pending = await pendingStore.PeekAsync(model.PendingId);
        if (pending == null)
            return InvalidRequest("Sign-in expired", "That sign-in request has expired. Go back to the application and start again.");

        if (!ModelState.IsValid)
            return SignInFailed(model, "Enter your ReLive ID and password.");

        var response = await ssoClient.SignInAsync(new SsoSignInRequest()
        {
            Username = model.Username,
            Password = model.Password,
            Persistent = model.RememberMe,
            UserAgent = Request.Headers.UserAgent.ToString(),
            CreatedIp = ClientIp() ?? "",
        }, cancellationToken: cancellationToken);

        if (response.Code != S_OK || string.IsNullOrEmpty(response.SessionHandle))
            return SignInFailed(model, "That ReLive ID or password isn't recognised.");

        SetSessionCookie(response.SessionHandle, model.RememberMe, response.Expires.ToDateTimeOffset());

        await pendingStore.TakeAsync(model.PendingId);

        var code = await IssueCodeAsync(response.SessionHandle, pending, cancellationToken);
        if (code == null)
            return InvalidRequest("Sign-in failed", "We could not complete that sign-in. Please try again.");

        return RedirectWithCode(pending.RedirectUri, pending.State, code);
    }

    [HttpPost("token")]
    [EnableRateLimiting("SsoToken")]
    public async Task<IActionResult> Token([FromBody] TokenRequestModel request, CancellationToken cancellationToken)
    {
        var client = Options.FindClient(request?.ClientId);
        if (request == null || client == null || !client.IsRedirectUriAllowed(request.RedirectUri))
            return BadRequest(new { error = "invalid_client" });

        var redeem = new RedeemAuthorizationCodeRequest()
        {
            AuthorizationCode = request.Code ?? "",
            ClientId = request.ClientId,
            RedirectUri = request.RedirectUri,
        };

        if (request.CodeVerifier is { Length: > 0 } verifier)
            redeem.CodeVerifier = verifier;

        var response = await ssoClient.RedeemAuthorizationCodeAsync(redeem, cancellationToken: cancellationToken);
        if (response.Code != S_OK)
            return BadRequest(new { error = "invalid_grant" });

        return Ok(AuthenticationController.ToTokensModel(response));
    }

    [HttpGet("signout")]
    public async Task<IActionResult> SignOutSession(
        string? client_id, string? post_logout_redirect_uri, CancellationToken cancellationToken)
    {
        var handle = Request.Cookies[Options.Cookie.Name];
        if (handle is { Length: > 0 })
        {
            await ssoClient.RevokeSessionAsync(
                new RevokeSessionRequest() { SessionHandle = handle }, cancellationToken: cancellationToken);
        }

        Response.Cookies.Delete(Options.Cookie.Name, new CookieOptions()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
        });

        var client = Options.FindClient(client_id);
        if (client != null && client.IsPostLogoutRedirectUriAllowed(post_logout_redirect_uri))
            return Redirect(post_logout_redirect_uri!);

        return View("SignedOut");
    }

    private async Task<IssuedCode?> IssueCodeAsync(string handle, PendingAuthorize pending, CancellationToken cancellationToken)
    {
        var request = new IssueAuthorizationCodeRequest()
        {
            SessionHandle = handle,
            ClientId = pending.ClientId,
            RedirectUri = pending.RedirectUri,
        };

        request.ServiceTargets.AddRange(pending.ServiceTargets);
        if (pending.CodeChallenge is { Length: > 0 })
        {
            request.CodeChallenge = pending.CodeChallenge;
            request.CodeChallengeMethod = pending.CodeChallengeMethod ?? "S256";
        }

        var response = await ssoClient.IssueAuthorizationCodeAsync(request, cancellationToken: cancellationToken);
        if (response.Code != S_OK)
        {
            logger.LogInformation("Authorization code not issued for client {Client}, code {Code:X8}",
                Options.FindClient(pending.ClientId)?.Name ?? pending.ClientId, response.Code);
            return null;
        }

        return new IssuedCode(response.AuthorizationCode, response.Persistent);
    }

    private void SetSessionCookie(string handle, bool persistent, DateTimeOffset expires)
    {
        Response.Cookies.Append(Options.Cookie.Name, handle, new CookieOptions()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = persistent ? expires : null,
        });
    }

    private IActionResult SignInFailed(SignInViewModel model, string error)
    {
        model.Password = "";
        model.Error = error;

        Response.StatusCode = StatusCodes.Status400BadRequest;
        return View("SignIn", model);
    }

    private IActionResult InvalidRequest(string title, string detail)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return View("Error", new SsoErrorViewModel(title, detail));
    }

    // the code goes in the fragment so it never reaches the app's own web server logs. persistent
    // rides along because the Remember me box lives on this page, and the app needs it to decide
    // between local and session storage.
    private IActionResult RedirectWithCode(string redirectUri, string state, IssuedCode issued)
        => Redirect($"{redirectUri}#code={Uri.EscapeDataString(issued.Code)}"
            + $"&state={Uri.EscapeDataString(state)}"
            + $"&persistent={(issued.Persistent ? "1" : "0")}");

    private IActionResult RedirectWithError(string redirectUri, string? state, string error, string description)
    {
        var target = $"{redirectUri}#error={Uri.EscapeDataString(error)}&error_description={Uri.EscapeDataString(description)}";
        if (state is { Length: > 0 })
            target += $"&state={Uri.EscapeDataString(state)}";

        return Redirect(target);
    }

    private record IssuedCode(string Code, bool Persistent);

    private string? ClientIp()
        => HttpContext.Connection.RemoteIpAddress?.ToString();

    private static string[] ParseScope(string? scope)
        => scope is { Length: > 0 }
            ? scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];
}
