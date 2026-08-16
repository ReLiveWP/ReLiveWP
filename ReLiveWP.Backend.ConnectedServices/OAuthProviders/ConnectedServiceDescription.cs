using ReLiveWP.Backend.ConnectedServices.Data;

namespace ReLiveWP.Backend.ConnectedServices.OAuthProviders;

public enum ServiceLinkMode
{
    OAuthRedirect,
    Credentials,
}

public class ConnectedServiceDescription
{
    public bool IsEnabled { get; set; } = true;
    public required string ServiceId { get; set; }
    public required string DisplayName { get; set; }
    public ServiceLinkMode LinkMode { get; set; } = ServiceLinkMode.OAuthRedirect;
    public string ClientId { get; set; } = "";
    public string Scopes { get; set; } = "";
    public string RedirectUri { get; set; } = "";
    public required LiveConnectedServiceCapabilities ServiceCapabilities { get; set; }
    public LiveConnectedServiceCapabilities ShareableCapabilities { get; set; } = LiveConnectedServiceCapabilities.None;
    public string? Issuer { get; set; }
    public string? ClientSecret { get; set; }
    public string? AuthorizationEndpoint { get; set; }
    public string? TokenEndpoint { get; set; }
    public Func<IServiceProvider, Task<IOAuthProvider>>? OAuthHandler { get; set; }
    public Func<IServiceProvider, Task<ICredentialLinkProvider>>? CredentialHandler { get; set; }
}
