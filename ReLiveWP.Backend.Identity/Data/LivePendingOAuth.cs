using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.Identity.Data;

[Index(nameof(State))]
public class LivePendingOAuth
{
    [Key]
    public required string State { get; set; }
    public required Guid UserId { get; set; }
    public LiveUser? User { get; set; }
    public required string Service { get; set; }
    public string? CodeVerifier { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public string? Endpoint { get; set; }
    public string? AuthorizationEndpoint { get; internal set; }
    public string? TokenEndpoint { get; internal set; }

    [NotMapped]
    public string? RedirectUri { get; set; }
}
