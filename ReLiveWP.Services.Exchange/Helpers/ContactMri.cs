namespace ReLiveWP.Services.Exchange.Helpers;

internal static class ContactMri
{
    private const string ShellContactTypeFan = "Fan";

    public static string Passport(string wlid) => "1:" + wlid;

    public static List<string> Derive(string? wlid, string? sourceId, string? objectId, string? shellContactType)
    {
        var mris = new List<string>();

        if (!string.IsNullOrEmpty(wlid))
            mris.Add(Passport(wlid));

        if (TryNetwork(sourceId, objectId, shellContactType, out var network))
            mris.Add(network);

        return mris;
    }

    private static bool TryNetwork(string? sourceId, string? objectId, string? shellContactType, out string mri)
    {
        mri = string.Empty;

        if (string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(objectId))
            return false;

        // a fan is a page you follow, not a person you can message
        if (string.Equals(shellContactType, ShellContactTypeFan, StringComparison.OrdinalIgnoreCase))
            return false;

        var domain = ContactDomains.BySource(sourceId);
        if (domain is not { IsImEnabled: true } || domain.MriGeneration != MriGeneration.ObjectIdBasedXmpp)
            return false;

        mri = $"13:{objectId};via=14:{sourceId.ToLowerInvariant()}";
        return true;
    }
}
