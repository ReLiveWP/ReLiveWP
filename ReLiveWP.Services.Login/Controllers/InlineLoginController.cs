using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Login.Models;
using ReLiveWP.Services.Login.Utilities;

namespace ReLiveWP.Services.Login.Controllers;

public class InlineLoginController(
    ILogger<InlineLoginController> logger,
    Authentication.AuthenticationClient authenticationClient,
    SupportLinks supportLinks,
    IOptionsMonitor<InlineLoginOptions> inlineLoginOptions) : Controller
{
    private const string BootstrapServiceTarget = "http://Passport.NET/tb";

    // TODO(wam): a real flow mints one of these per session, a straight sign-in never replays it
    private const string StaticStsInlineFlowToken = "-Duj0!xC4I*UcMjptEuiPqI7Wkr9B53Yjdsypyodu2NRXcB5TvESw3jJWwMEjMVCeKMF289Qm6q5MtkGHalfq5GCj1ynTj8sBOiIDVlKqF*Bm";

    // InlineConnect is the Windows 8.1 entry point and posts a form body, InlineLogin is the phone's
    // and gets. same page either way, so don't redirect between them, a 301 would drop the body
    [HttpGet]
    [HttpPost]
    [ActionName("InlineLogin")]
    [Route("/ppsecure/InlineConnect.srf")]
    [Route("/ppsecure/InlineLogin.srf")]
    public IActionResult InlineLogin(InlineLoginContext context)
    {
        return View("InlineLogin", ToModel(context));
    }

    [HttpPost]
    [Route("/ppsecure/post.srf")]
    [EnableRateLimiting("AuthTokens")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(
        InlineLoginContext context,
        [FromForm(Name = "loginfmt")] string? loginfmt,
        [FromForm(Name = "passwd")] string? passwd,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(loginfmt) || string.IsNullOrEmpty(passwd))
            return Retry(context, loginfmt, PassportErrors.BadMemberNameOrPassword);

        var request = new SecurityTokensRequest() { Username = loginfmt, Password = passwd };
        request.Requests.Add(new SecurityTokenRequest()
        {
            ServiceTarget = BootstrapServiceTarget,
            ServicePolicy = "LEGACY"
        });

        var response = await authenticationClient.GetSecurityTokensAsync(request, cancellationToken: cancellationToken);
        if (((int)response.Code) < 0)
            return Retry(context, loginfmt, response.Code);

        var bootstrap = response.Tokens.FirstOrDefault(t => t.ServiceTarget == BootstrapServiceTarget);
        if (bootstrap is null || !bootstrap.HasProofKey)
        {
            logger.LogWarning("InlineLogin: no bootstrap token with a proof key for {Username}", loginfmt);
            return Retry(context, loginfmt, PassportErrors.Unauthenticated);
        }

        var properties = BuildProperties(context, response, bootstrap, passwd);
        logger.LogInformation("InlineLogin: pushing {Count} properties to the host ({Names})",
            properties.Count, string.Join(", ", properties.Keys));

        return View("Success", new InlineLoginSuccessModel(properties));
    }

    private Dictionary<string, string> BuildProperties(InlineLoginContext context, SecurityTokensResponse response, SecurityTokenResponse bootstrap, string password)
    {
        var options = inlineLoginOptions.CurrentValue;
        var created = bootstrap.Created.ToDateTimeOffset();

        // desktop hosts reject every DA property by name and want the credentials packed into an
        // auth identity instead
        if (!context.IsPhone)
        {
            // wlidsvc requires a member name shaped like an email, one @ and not at either end,
            // and rejects anything else before it goes near the network
            var member = string.IsNullOrEmpty(response.EmailAddress) ? response.Username : response.EmailAddress;

            // no Password: lsasrv logs the *local* account on with it, and the wizard's own
            // confirm-password page is what collects that
            var desktopProperties = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Username"] = member,
                ["AuthenticationBuffer"] = AuthIdentity.PackPassword(member, password),
            };

            return ApplyOverrides(desktopProperties, options);
        }

        var daToken = options.DaTokenForm switch
        {
            DaTokenForm.CipherValue => bootstrap.Token,
            DaTokenForm.WireForm => PassportSoap.DaTokenWireForm(bootstrap.Token, created),
            _ => PassportSoap.BinaryDaTokenEnvelope(bootstrap.Token)
        };

        var puid = options.PuidFormat switch
        {
            PuidFormat.Decimal => response.Puid.ToString(),
            _ => response.Puid.ToString("X16")
        };

        var properties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["DAToken"] = daToken,
            ["DASessionKey"] = bootstrap.ProofKey.ToBase64(),
            ["DAStartTime"] = PassportSoap.FormatZ(created),
            ["DAExpires"] = PassportSoap.FormatZ(bootstrap.Expires.ToDateTimeOffset()),
            ["STSInlineFlowToken"] = StaticStsInlineFlowToken,
            ["CID"] = response.Cid,
            ["PUID"] = puid,
            ["Username"] = response.Username,
            ["SigninName"] = response.Username,
        };

        return ApplyOverrides(properties, options);
    }

    private static Dictionary<string, string> ApplyOverrides(Dictionary<string, string> properties, InlineLoginOptions options)
    {
        foreach (var name in options.Suppress)
            properties.Remove(name);

        foreach (var (name, value) in options.Extra)
            properties[name] = value;

        return properties;
    }

    private static InlineLoginModel ToModel(InlineLoginContext context)
        => new(context, Win8Palette.Parse(context.Win8Colors));

    private ViewResult Retry(InlineLoginContext context, string? username, uint code)
        => View("InlineLogin", ToModel(context) with
        {
            Username = username ?? "",
            ErrorCode = code,
            Error = PassportErrors.Describe(code),
            HelpUrl = supportLinks.ForErrorCode(code)
        });
}
