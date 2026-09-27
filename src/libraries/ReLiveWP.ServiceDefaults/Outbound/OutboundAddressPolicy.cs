using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ReLiveWP.ServiceDefaults.Outbound;

public interface IOutboundAddressPolicy
{
    bool IsAllowed(IPAddress address);

    bool IsAllowedScheme(string scheme);
}

public sealed class PublicOnlyAddressPolicy : IOutboundAddressPolicy
{
    private static readonly IPNetwork[] BlockedNetworks =
    [
        IPNetwork.Parse("0.0.0.0/8"),
        IPNetwork.Parse("10.0.0.0/8"),
        IPNetwork.Parse("100.64.0.0/10"),
        IPNetwork.Parse("127.0.0.0/8"),
        IPNetwork.Parse("169.254.0.0/16"),
        IPNetwork.Parse("172.16.0.0/12"),
        IPNetwork.Parse("192.0.0.0/24"),
        IPNetwork.Parse("192.168.0.0/16"),
        IPNetwork.Parse("198.18.0.0/15"),
        IPNetwork.Parse("224.0.0.0/3"),
        IPNetwork.Parse("::/128"),
        IPNetwork.Parse("::1/128"),
        IPNetwork.Parse("fe80::/10"),
        IPNetwork.Parse("fec0::/10"),
        IPNetwork.Parse("ff00::/8"),
        IPNetwork.Parse("fc00::/7"),
        IPNetwork.Parse("2001:db8::/32"),
    ];

    public bool IsAllowedScheme(string scheme)
        => string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    public bool IsAllowed(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (address.AddressFamily is not (AddressFamily.InterNetwork or AddressFamily.InterNetworkV6))
            return false;

        return !BlockedNetworks.Any(network => network.Contains(address));
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

    public static IHttpClientBuilder AddGuardedOutboundClient(this IServiceCollection services)
    {
        services.TryAddSingleton<IOutboundAddressPolicy, PublicOnlyAddressPolicy>();

        return services.AddHttpClient(GuardedClientName)
            .ConfigurePrimaryHttpMessageHandler(s => CreateGuardedHandler(s.GetRequiredService<IOutboundAddressPolicy>()));
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
