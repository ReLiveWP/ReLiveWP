using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Identity.Data;

namespace ReLiveWP.Backend.Identity.Data;

public class LiveConnectedService
{
    [Key]
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public LiveUser? User { get; set; }
    public required string Service { get; set; }
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public required LiveConnectedServiceFlags Flags { get; set; }
         = LiveConnectedServiceFlags.None;
    public required LiveConnectedServiceCapabilities EnabledCapabilities { get; set; }
    public string? DPoPKeyId { get; set; }
    public string? ServiceUrl { get; set; }
    public string? AuthorizationEndpoint { get; set; }
    public string? TokenEndpoint { get; set; }
    public string? Issuer { get; set; }

    public LiveConnectedServiceProfile ServiceProfile { get; set; }
        = new LiveConnectedServiceProfile();
}

[Owned]
public class LiveConnectedServiceProfile
{
    public string UserId { get; set; } = default!;
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? EmailAddress { get; set; }
    public string? AvatarUrl { get; set; }
}