using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Providers;

using ServiceCaps = ReLiveWP.Backend.ConnectedServices.Data.LiveConnectedServiceCapabilities;
using GoogleService = ReLiveWP.Backend.ConnectedServices.Providers.Google;
using MicrosoftService = ReLiveWP.Backend.ConnectedServices.Providers.Microsoft;
using ReLiveWP.Backend.ConnectedServices.Services;

namespace ReLiveWP.Backend.ConnectedServices.Tests;

public class OidcRevocationTests
{
    private readonly FakeFediverseHandler server = new();
    private readonly FakeHttpClientFactory factory;
    private readonly ConnectedServicesContainer container = new();

    public OidcRevocationTests()
    {
        factory = new FakeHttpClientFactory(server);

        container[GoogleService.SERVICE_NAME] = Describe(GoogleService.SERVICE_NAME, "Google");
        container[MicrosoftService.SERVICE_NAME] = Describe(MicrosoftService.SERVICE_NAME, "Microsoft");
    }

    [Fact]
    public async Task Google_revokes_the_refresh_token_which_ends_the_whole_grant()
    {
        server.OnJson(HttpMethod.Get, "https://accounts.google.com/.well-known/openid-configuration", """
            {
              "issuer": "https://accounts.google.com",
              "authorization_endpoint": "https://accounts.google.com/o/oauth2/v2/auth",
              "token_endpoint": "https://oauth2.googleapis.com/token",
              "revocation_endpoint": "https://oauth2.googleapis.com/revoke",
              "jwks_uri": "https://www.googleapis.com/oauth2/v3/certs"
            }
            """);
        server.OnJson(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/certs", """{"keys":[]}""");
        server.OnJson(HttpMethod.Post, "https://oauth2.googleapis.com/revoke", "{}");

        var provider = new GoogleOAuthProvider(container, factory, NullLogger<GoogleOAuthProvider>.Instance);
        await provider.RevokeTokensAsync(Connection(GoogleService.SERVICE_NAME));

        var revoke = Assert.Single(server.Requests, r => r.Method == HttpMethod.Post);
        Assert.Contains("token=the-refresh-token", revoke.Body);
        Assert.Contains("token_type_hint=refresh_token", revoke.Body);
        Assert.Contains("client_id=client-id", revoke.Body);
    }

    [Fact]
    public async Task A_provider_without_a_revocation_endpoint_sends_nothing()
    {
        server.OnJson(HttpMethod.Get, MicrosoftService.DISCOVERY_URL, """
            {
              "issuer": "https://login.microsoftonline.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0",
              "authorization_endpoint": "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize",
              "token_endpoint": "https://login.microsoftonline.com/consumers/oauth2/v2.0/token",
              "jwks_uri": "https://login.microsoftonline.com/consumers/discovery/v2.0/keys"
            }
            """);
        server.OnJson(HttpMethod.Get, "https://login.microsoftonline.com/consumers/discovery/v2.0/keys", """{"keys":[]}""");

        var provider = new MicrosoftOAuthProvider(container, factory, NullLogger<MicrosoftOAuthProvider>.Instance);
        await provider.RevokeTokensAsync(Connection(MicrosoftService.SERVICE_NAME));

        // the key set being fetched proves discovery succeeded, so nothing was posted for want of an endpoint
        Assert.Contains(server.Requests, r => r.Uri.AbsolutePath.EndsWith("/keys"));
        Assert.DoesNotContain(server.Requests, r => r.Method == HttpMethod.Post);
    }

    private static ConnectedServiceDescription Describe(string serviceId, string displayName) => new()
    {
        ServiceId = serviceId,
        DisplayName = displayName,
        ClientId = "client-id",
        ClientSecret = "client-secret",
        ServiceCapabilities = ServiceCaps.Contacts,
    };

    private static LiveConnectedService Connection(string service) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Service = service,
        AccessToken = "the-access-token",
        RefreshToken = "the-refresh-token",
        ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        Flags = LiveConnectedServiceFlags.None,
        AvailableCapabilities = ServiceCaps.Contacts,
        EnabledCapabilities = ServiceCaps.Contacts,
    };
}
