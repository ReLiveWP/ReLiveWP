using System.Text.Json;
using Duende.IdentityModel.Client;
using ReLiveWP.Backend.ConnectedServices.Data;

using static ReLiveWP.Backend.ConnectedServices.OAuthProviders.Microsoft;

namespace ReLiveWP.Backend.ConnectedServices.OAuthProviders;

public class MicrosoftOAuthProvider(IConnectedServicesContainer connectedServices,
                                    IHttpClientFactory httpClientFactory,
                                    ILogger<MicrosoftOAuthProvider> logger)
    : OidcOAuthProvider(SERVICE_NAME, DISCOVERY_URL, connectedServices, httpClientFactory, logger)
{
    protected override DiscoveryPolicy DiscoveryPolicy
        => new() { ValidateEndpoints = false, ValidateIssuerName = false };

    // microsoft doesn't echo `scope` back on the authorize redirect the way google does, so the
    // caller's list is empty on a normal link and the granted scopes have to come off the token.
    protected override LiveConnectedServiceCapabilities GetGrantedCapabilities(string[] authorizedScopes, TokenResponse tokenResult)
        => GetCapabilitiesFromScopes(authorizedScopes.Length > 0
            ? authorizedScopes
            : (tokenResult.Scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));

    protected override async Task FetchUserInfoAsync(LiveConnectedService service)
    {
        using var client = HttpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + service.AccessToken);

        var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me");
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;

        service.ServiceProfile.UserId = root.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
        service.ServiceProfile.DisplayName = root.TryGetProperty("displayName", out var dn) ? dn.GetString() : null;
        service.ServiceProfile.Username = root.TryGetProperty("displayName", out var un) ? un.GetString() : null;
        service.ServiceProfile.EmailAddress = root.TryGetProperty("mail", out var mail) ? mail.GetString()
            : root.TryGetProperty("userPrincipalName", out var upn) ? upn.GetString() : null;
    }

    private const string ScopePrefix = "https://graph.microsoft.com/";

    internal static LiveConnectedServiceCapabilities GetCapabilitiesFromScopes(string[] scopes)
    {
        LiveConnectedServiceCapabilities caps = 0;

        foreach (var raw in scopes)
        {
            var scope = raw.Trim();
            if (!scope.StartsWith(ScopePrefix, StringComparison.Ordinal)) continue;

            caps |= scope[ScopePrefix.Length..].Split('.')[0] switch
            {
                "Files" => LiveConnectedServiceCapabilities.FileStorage | LiveConnectedServiceCapabilities.PhotoSync,
                "Contacts" => LiveConnectedServiceCapabilities.Contacts,
                "Calendars" => LiveConnectedServiceCapabilities.Calendar,
                _ => 0,
            };
        }

        return caps;
    }
}
