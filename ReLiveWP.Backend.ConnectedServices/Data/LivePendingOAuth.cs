using System.Text.Json.Serialization;

namespace ReLiveWP.Backend.ConnectedServices.Data;

public class LivePendingOAuth
{
    public required string State { get; set; }
    public required Guid UserId { get; set; }
    public required string Service { get; set; }
    public string? CodeVerifier { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public string? Endpoint { get; set; }
    public string? AuthorizationEndpoint { get; set; }
    public string? TokenEndpoint { get; set; }
    public Guid? ExistingConnectionId { get; set; }
    public string? RedirectUri { get; set; }
    public bool Transient { get; set; }
}
