using System.ComponentModel.DataAnnotations;

namespace ReLiveWP.Backend.ConnectedServices.Data;

public class LiveOAuthClient
{
    [Key]
    public required string Authority { get; set; }
    public required string Service { get; set; }
    public required string ClientId { get; set; }
    public required string EncryptedSecret { get; set; }
    public required string RedirectUri { get; set; }
    public required string RequestedScopes { get; set; }
    public required string RegisteredScopes { get; set; }
    public required string AuthorizationEndpoint { get; set; }
    public required string TokenEndpoint { get; set; }
    public string? RevocationEndpoint { get; set; }
    public required DateTimeOffset RegisteredAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
