using ReLiveWP.Backend.Chat.Data;
using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Backend.Chat.Utilities;

public static class PresenceRules
{
    public static bool IsUserChoice(PresenceStatus status) =>
        status is not (PresenceStatus.Idle or PresenceStatus.Offline);

    public static bool IsPresent(ChatEndpoint endpoint) =>
        endpoint.Active && endpoint.Status != PresenceStatus.Idle;

    public static PresenceStatus Show(PresenceStatus chosen, IReadOnlyCollection<ChatEndpoint> endpoints)
    {
        if (endpoints.Count == 0)
            return PresenceStatus.Offline;

        return chosen switch
        {
            PresenceStatus.Hidden or PresenceStatus.Offline => PresenceStatus.Offline,
            PresenceStatus.Online when !endpoints.Any(IsPresent) => PresenceStatus.Idle,
            _ => chosen,
        };
    }
}
