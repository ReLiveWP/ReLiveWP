namespace ReLiveWP.Services.Messenger.Data;

public class MsnpGatewaySession
{
    public string SessionId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Nonce { get; set; } = null!;
    public string Policy { get; set; } = null!;
    public string NotificationUri { get; set; } = null!;
    public int? SessionTimeoutSeconds { get; set; }
    public MsnpGatewaySessionState State { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    // presence, TODO: actual presence handling
    public Guid? EndpointId { get; set; }
    public string? Status { get; set; }

    // authed user
    public string? UserId { get; set; }
    public long? Puid { get; set; }
    public string? Cid { get; set; }

    public bool ChatRegistered { get; set; }
    public bool ChatSuperseded { get; set; }
}
