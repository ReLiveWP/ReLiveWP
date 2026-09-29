using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Services.Messenger.Msnp;

public static class MsnpPresenceStatus
{
    private static readonly Dictionary<string, PresenceStatus> FromWire = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FLN"] = PresenceStatus.Offline,
        ["NLN"] = PresenceStatus.Online,
        ["BSY"] = PresenceStatus.Busy,
        ["AWY"] = PresenceStatus.Away,
        ["BRB"] = PresenceStatus.BeRightBack,
        ["PHN"] = PresenceStatus.OnThePhone,
        ["LUN"] = PresenceStatus.OutToLunch,
        ["IDL"] = PresenceStatus.Idle,
        ["HDN"] = PresenceStatus.Hidden,
    };

    private static readonly Dictionary<PresenceStatus, string> ToWire =
        FromWire.ToDictionary(pair => pair.Value, pair => pair.Key);

    public static bool TryParse(string? wire, out PresenceStatus status)
    {
        status = PresenceStatus.Offline;
        return wire is not null && FromWire.TryGetValue(wire, out status);
    }

    public static string Format(PresenceStatus status) => ToWire.GetValueOrDefault(status, "NLN");
}
