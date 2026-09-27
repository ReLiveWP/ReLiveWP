using Duende.IdentityModel.Client;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using ReLiveWP.Backend.ConnectedServices.Data;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

using static ReLiveWP.Backend.ConnectedServices.Providers.Google;
using ReLiveWP.Backend.ConnectedServices.Services;

namespace ReLiveWP.Backend.ConnectedServices.Providers;

public class GoogleOAuthProvider(IConnectedServicesContainer connectedServices,
                                 IHttpClientFactory httpClientFactory,
                                 ILogger<GoogleOAuthProvider> logger)
    : OidcOAuthProvider(SERVICE_NAME, DISCOVERY_URL, connectedServices, httpClientFactory, logger)
{
    protected override Parameters ExtraAuthorizeParameters
        => new() { { "access_type", "offline" }, { "prompt", "consent" } };

    protected override LiveConnectedServiceCapabilities GetGrantedCapabilities(string[] authorizedScopes, TokenResponse tokenResult)
        => GetCapabilitiesFromScopes(authorizedScopes);

    protected override async Task FetchUserInfoAsync(LiveConnectedService service)
    {
        using var oauthService = new Oauth2Service(new BaseClientService.Initializer()
        {
            ApplicationName = "ReLiveWP Connected Services"
        });

        oauthService.HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + service.AccessToken);

        var response = await oauthService.Userinfo.Get()
            .ExecuteAsync();

        service.ServiceProfile.UserId = response.Id;
        service.ServiceProfile.Username = response.Name;
        service.ServiceProfile.DisplayName = response.Name;
        service.ServiceProfile.AvatarUrl = response.Picture;
        service.ServiceProfile.EmailAddress = response.Email;
    }

    private const string ScopePrefix = "https://www.googleapis.com/auth/";

    internal static LiveConnectedServiceCapabilities GetCapabilitiesFromScopes(string[] scopes)
    {
        LiveConnectedServiceCapabilities caps = 0;

        foreach (var raw in scopes)
        {
            var scope = raw.Trim();
            if (!scope.StartsWith(ScopePrefix, StringComparison.Ordinal)) continue;

            var name = scope[ScopePrefix.Length..];

            caps |= name.Split('.')[0] switch
            {
                "contacts" => LiveConnectedServiceCapabilities.Contacts,
                "calendar" => LiveConnectedServiceCapabilities.Calendar,
                "drive" => LiveConnectedServiceCapabilities.FileStorage,
                "photoslibrary" => LiveConnectedServiceCapabilities.PhotoSync,
                "gmail" => LiveConnectedServiceCapabilities.Email,
                _ => 0,
            };
        }

        return caps;
    }
}
