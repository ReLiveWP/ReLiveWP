using System.Web;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Logging;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Identity.Soap;

public enum SoapTicketStatus
{
    Valid,
    Missing,
    ServiceError,
    Invalid,
}

public sealed record SoapTicketResult(SoapTicketStatus Status, string UserId, IReadOnlyDictionary<string, string> Claims)
{
    public bool IsValid => Status == SoapTicketStatus.Valid;

    public string FailureMessage => Status switch
    {
        SoapTicketStatus.Missing => "Missing TicketToken.",
        SoapTicketStatus.ServiceError => "Authentication service error.",
        SoapTicketStatus.Invalid => "Invalid TicketToken.",
        _ => "",
    };

    public string? Claim(string type)
        => Claims.TryGetValue(type, out var value) && !string.IsNullOrEmpty(value) ? value : null;

    internal static SoapTicketResult Failed(SoapTicketStatus status)
        => new(status, "", new Dictionary<string, string>());
}

// SOAP clients put the ticket in a header (ABAuthHeader, SOAPUserHeader) instead of the HTTP
// Authorization header LiveIDAuthHandler reads, so asmx services verify it themselves with this.
// TODO: there is 100% a better way to do this, i refuse to believe SoapCore doesn't
// offer an auth mechanism of its own
public class SoapTicketVerifier(GrpcClientFactory grpcClientFactory, ILogger<SoapTicketVerifier> logger)
{
    internal const string ClientName = "Identity_SoapTicketClient";

    private readonly Authentication.AuthenticationClient authentication
        = grpcClientFactory.CreateClient<Authentication.AuthenticationClient>(ClientName);

    public async Task<SoapTicketResult> VerifyAsync(
        string? ticketToken, IEnumerable<string> serviceTargets, CancellationToken ct = default)
    {
        var jwt = ExtractJwt(ticketToken);
        if (jwt == null)
            return SoapTicketResult.Failed(SoapTicketStatus.Missing);

        var request = new VerifyTokenRequest { Token = jwt, TokenType = "JWT" };
        request.ServiceTargets.Add(serviceTargets);

        VerifyResponse reply;
        try
        {
            reply = await authentication.VerifySecurityTokenAsync(request, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate TicketToken.");
            return SoapTicketResult.Failed(SoapTicketStatus.ServiceError);
        }

        if (reply.Code != 0)
        {
            logger.LogWarning("TicketToken validation failed with code {Code:X}.", reply.Code);
            return SoapTicketResult.Failed(SoapTicketStatus.Invalid);
        }

        var claims = reply.Claims.ToDictionary(c => c.Type, c => c.Value);
        return new SoapTicketResult(SoapTicketStatus.Valid, reply.Id, claims);
    }

    // "t=<jwt>&p=", html-encoded once more on top of the XML escaping
    public static string? ExtractJwt(string? ticketToken)
    {
        var decoded = HttpUtility.HtmlDecode(ticketToken);
        if (string.IsNullOrWhiteSpace(decoded))
            return null;

        var jwt = HttpUtility.ParseQueryString(decoded)["t"];
        return string.IsNullOrWhiteSpace(jwt) ? null : jwt;
    }
}
