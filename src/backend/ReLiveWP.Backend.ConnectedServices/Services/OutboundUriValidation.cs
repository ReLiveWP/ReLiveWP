using System.Net;
using ReLiveWP.Backend.ConnectedServices.Providers;
using ReLiveWP.ServiceDefaults.Outbound;

namespace ReLiveWP.Backend.ConnectedServices.Services;

public static class OutboundUriValidation
{
    public static void ValidateUri(this IOutboundAddressPolicy policy, Uri uri)
    {
        if (!policy.IsAllowedScheme(uri.Scheme))
            throw new CredentialLinkException("The server address must use https.");

        if (IPAddress.TryParse(uri.DnsSafeHost, out var literal) && !policy.IsAllowed(literal))
            throw new CredentialLinkException("The server must be reachable at a public address.");
    }
}
