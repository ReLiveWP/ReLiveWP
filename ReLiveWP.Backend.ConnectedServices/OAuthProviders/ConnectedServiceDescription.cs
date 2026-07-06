using ReLiveWP.Backend.ConnectedServices.Data;

namespace ReLiveWP.Backend.ConnectedServices.OAuthProviders;

public class ConnectedServiceDescription
{
    public bool IsEnabled { get; set; } = true;
    public required string ServiceId { get; set; }
    public required string DisplayName { get; set; }
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
