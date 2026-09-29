using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Backend.Chat.Data;

public record ChatEndpoint(
    string EndpointId,
    string UserId,
    string Address,
    PresenceStatus Status,
    bool Active,
    TimeSpan SessionTimeout,
    DateTimeOffset LastActivity,
    string InstanceId = "");

public record ChatUser(string UserId, string Address, PresenceStatus Shown, PresenceStatus Chosen);

public record PresenceAudience(IReadOnlySet<string> CanSee, IReadOnlySet<string> SeenBy)
{
    public static PresenceAudience Empty { get; } = new(new HashSet<string>(), new HashSet<string>());
}

public enum AudienceSide
{
    CanSee,
    SeenBy,
}
