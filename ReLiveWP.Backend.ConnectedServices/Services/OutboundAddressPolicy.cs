using System.Net;
using System.Net.Sockets;
using ReLiveWP.Backend.ConnectedServices.OAuthProviders;

namespace ReLiveWP.Backend.ConnectedServices.Services;

public interface IOutboundAddressPolicy
{
    bool IsAllowed(IPAddress address);

    bool IsAllowedScheme(string scheme);
}

public sealed class PublicOnlyAddressPolicy : IOutboundAddressPolicy
{
    public bool IsAllowedScheme(string scheme)
        => string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    public bool IsAllowed(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsAllowedV4(address),
            AddressFamily.InterNetworkV6 => IsAllowedV6(address),
            _ => false,
        };
    }

    private static bool IsAllowedV4(IPAddress address)
    {
        Span<byte> octets = stackalloc byte[4];
        if (!address.TryWriteBytes(octets, out _))
            return false;

        return (octets[0], octets[1], octets[2]) switch
        {
            (0, _, _) => false,
            (10, _, _) => false,
            (100, >= 64 and <= 127, _) => false,
            (127, _, _) => false,
            (169, 254, _) => false,
            (172, >= 16 and <= 31, _) => false,
            (192, 0, 0) => false,
            (192, 168, _) => false,
            (198, 18 or 19, _) => false,
            (>= 224, _, _) => false,
            _ => true,
        };
    }

    private static bool IsAllowedV6(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal ||
            address.IsIPv6SiteLocal || address.IsIPv6Multicast ||
            address.Equals(IPAddress.IPv6Any))
            return false;

        Span<byte> bytes = stackalloc byte[16];
        if (!address.TryWriteBytes(bytes, out _))
            return false;

        // fc00::/7 unique local
        if ((bytes[0] & 0xFE) == 0xFC)
            return false;

        // 2001:db8::/32 documentation
        if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0D && bytes[3] == 0xB8)
            return false;

        return true;
    }
}

public sealed class UnrestrictedAddressPolicy : IOutboundAddressPolicy
{
    public bool IsAllowedScheme(string scheme)
        => string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
           string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);

    public bool IsAllowed(IPAddress address) => true;
}

public static class OutboundAddressPolicyExtensions
{
    public const string GuardedClientName = "GuardedOutbound";

    public static void ValidateUri(this IOutboundAddressPolicy policy, Uri uri)
    {
        if (!policy.IsAllowedScheme(uri.Scheme))
            throw new CredentialLinkException("The server address must use https.");

        if (IPAddress.TryParse(uri.DnsSafeHost, out var literal) && !policy.IsAllowed(literal))
            throw new CredentialLinkException("The server must be reachable at a public address.");
    }

    public static SocketsHttpHandler CreateGuardedHandler(IOutboundAddressPolicy policy)
        => new()
        {
            AllowAutoRedirect = false,
            ConnectCallback = async (context, ct) =>
            {
                var resolved = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
                var permitted = resolved.Where(policy.IsAllowed).ToArray();

                if (permitted.Length == 0)
                    throw new HttpRequestException(
                        $"'{context.DnsEndPoint.Host}' does not resolve to a public address.");

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(permitted, context.DnsEndPoint.Port, ct);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            },
        };
}
