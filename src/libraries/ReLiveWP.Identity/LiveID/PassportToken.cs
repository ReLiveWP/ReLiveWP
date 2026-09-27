namespace ReLiveWP.Identity.LiveID;

public static class PassportToken
{
    public const string Scheme = "Passport1.4";

    private const string Marker = "from-PP";

    public static bool TryUnwrap(string value, out string token)
    {
        token = "";

        var start = value.IndexOf(Marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return false;

        var rest = value[(start + Marker.Length)..].TrimStart();

        if (rest.StartsWith('='))
            rest = rest[1..].TrimStart();

        if (rest.StartsWith('\''))
            rest = rest[1..];

        var end = rest.IndexOf('\'');
        token = (end < 0 ? rest : rest[..end]).Trim();

        return token.Length > 0;
    }
}
