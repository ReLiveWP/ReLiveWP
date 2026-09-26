using System.Globalization;

namespace ReLiveWP.ServiceDefaults.Outbound;

public readonly record struct FediverseHandle(string? Username, string Domain)
{
    private const int MaxLength = 320;
    private static readonly char[] InvalidUsernameChars = ['@', '/', '\\', '?', '#', ':', '%'];

    public Uri DomainRoot => new UriBuilder(Uri.UriSchemeHttps, Domain).Uri;

    public string? AccountAddress => Username == null ? null : $"{Username}@{Domain}";

    public static bool TryParse(string? input, out FediverseHandle handle)
    {
        handle = default;

        var trimmed = input?.Trim().TrimStart('@') ?? "";
        if (trimmed.Length == 0 || trimmed.Length > MaxLength || trimmed.Any(IsUnprintable))
            return false;

        if (trimmed.Contains("://", StringComparison.Ordinal))
            return TryParseInstanceUrl(trimmed, out handle);

        string? username = null;
        var domain = trimmed;

        var separator = trimmed.LastIndexOf('@');
        if (separator >= 0)
        {
            username = trimmed[..separator];
            domain = trimmed[(separator + 1)..];

            if (username.Length == 0 || username.IndexOfAny(InvalidUsernameChars) >= 0)
                return false;
        }

        if (!ExternalRequestGuard.TryCreateInstanceRoot(domain, out var root))
            return false;

        handle = new FediverseHandle(username, root.IdnHost.ToLowerInvariant());
        return true;
    }

    // prefer the server's own idea of the handle (fqn), it knows the handle domain on split-domain setups
    public static string DescribeAccountAddress(string username, string? fqn, string? acct, Uri instance)
    {
        if (TryParse(fqn, out var fromFqn) && string.Equals(fromFqn.Username, username, StringComparison.OrdinalIgnoreCase))
            return fromFqn.AccountAddress!;

        if (acct != null && acct.Contains('@') && TryParse(acct, out var fromAcct) && fromAcct.AccountAddress != null)
            return fromAcct.AccountAddress;

        return $"{username}@{instance.IdnHost}";
    }

    private static bool IsUnprintable(char c)
        => char.IsWhiteSpace(c) || char.IsControl(c) || char.GetUnicodeCategory(c) == UnicodeCategory.Format;

    private static bool TryParseInstanceUrl(string input, out FediverseHandle handle)
    {
        handle = default;

        if (!ExternalRequestGuard.TryParseAcceptableUri(input, out var uri) ||
            uri.AbsolutePath != "/" ||
            uri.Query.Length > 0 ||
            uri.Fragment.Length > 0)
            return false;

        handle = new FediverseHandle(null, uri.IdnHost.ToLowerInvariant());
        return true;
    }
}
