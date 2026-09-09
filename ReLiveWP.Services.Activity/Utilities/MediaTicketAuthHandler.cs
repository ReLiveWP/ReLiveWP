using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using ReLiveWP.Identity;
using ReLiveWP.Services.Activity.Services;

namespace ReLiveWP.Services.Activity.Utilities;

public class MediaTicketAuthHandler(MediaTicketService tickets,
                                    IOptionsMonitor<AuthenticationSchemeOptions> options,
                                    ILoggerFactory loggerFactory,
                                    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "MediaTicket";
    public const string OrLiveID = IdentityExtensions.LiveIDScheme + "," + SchemeName;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Query.TryGetValue(MediaTicketService.QueryKey, out var ticket) || string.IsNullOrEmpty(ticket))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (Request.RouteValues["resourceRef"] is not string resourceRef)
            return Task.FromResult(AuthenticateResult.Fail("Nothing on this route takes a media ticket."));

        var size = Request.RouteValues["size"] is string text
                   && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

        if (!tickets.TryVerifyTicket(ticket.ToString(), resourceRef, size, out var userId))
            return Task.FromResult(AuthenticateResult.Fail("Invalid media ticket."));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], SchemeName);
        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
