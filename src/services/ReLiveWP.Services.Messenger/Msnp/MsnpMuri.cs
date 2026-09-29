using System.Globalization;

namespace ReLiveWP.Services.Messenger.Msnp;

public readonly record struct MsnpMuri(int Type, string Address, Guid? Epid)
{
    public const int WindowsLiveType = 1;
    public const int GroupChatType = 10;

    public static MsnpMuri ForWindowsLive(string address, Guid? epid = null) => new(WindowsLiveType, address, epid);

    public static bool TryParse(string? value, out MsnpMuri muri)
    {
        muri = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var colon = value.IndexOf(':');
        if (colon <= 0 || !int.TryParse(value.AsSpan(0, colon), NumberStyles.None, CultureInfo.InvariantCulture, out var type))
            return false;

        var parts = value[(colon + 1)..].Split(';');
        var address = parts[0].Trim();
        if (address.Length == 0)
            return false;

        Guid? epid = null;
        foreach (var part in parts.AsSpan(1))
        {
            var eq = part.IndexOf('=');
            if (eq < 0 || !part[..eq].Trim().Equals("epid", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!TryParseEpid(part[(eq + 1)..].Trim(), out var parsed))
                return false;

            epid = parsed;
        }

        muri = new MsnpMuri(type, address, epid);
        return true;
    }

    public static bool TryParseEpid(string value, out Guid epid) =>
        Guid.TryParseExact(value, "B", out epid) || Guid.TryParseExact(value, "D", out epid);

    public static string FormatEpid(Guid epid) => epid.ToString("B").ToUpperInvariant();

    public override string ToString()
    {
        var muri = string.Create(CultureInfo.InvariantCulture, $"{Type}:{Address}");
        return Epid is { } epid ? $"{muri};epid={FormatEpid(epid)}" : muri;
    }
}
