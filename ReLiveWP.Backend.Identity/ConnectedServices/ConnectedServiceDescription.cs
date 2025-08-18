using Microsoft.AspNetCore.Authentication.OAuth;
using ReLiveWP.Backend.Identity.Data;

namespace ReLiveWP.Backend.Identity.ConnectedServices;

public class ConnectedServiceDescription
{
    public bool IsEnabled { get; set; } = true;
    public required string ServiceId { get; set; }
    public required string DisplayName { get; set; } // TODO: localisation is cool
    public required string ClientId { get; set; }
    public required string Scopes { get; set; }
    public required string RedirectUri { get; set; }
    public required LiveConnectedServiceCapabilities ServiceCapabilities { get; set; }
    public string? Issuer { get; set; }
    public string? ClientSecret { get; set; }
    public string? AuthorizationEndpoint { get; set; }
    public string? TokenEndpoint { get; set; }
    public required Func<IServiceProvider, Task<IOAuthProvider>> OAuthHandler { get; set; }
}
