using System.Net;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Providers;

namespace ReLiveWP.Backend.ConnectedServices.Tests;

public class MastodonClientRegistryTests : IDisposable
{
    private static readonly Uri Snug = new("https://snug.moe/");

    private readonly MastodonTestBed bed = new();

    public void Dispose() => bed.Dispose();

    [Fact]
    public async Task A_server_without_oauth_metadata_registers_on_the_fixed_paths()
    {
        bed.ServeInstance("snug.moe");
        bed.ServeAppRegistration("snug.moe");

        using var db = bed.NewContext();
        var client = await bed.NewRegistry(db).GetOrRegisterClientAsync(Snug);

        Assert.Equal("client-id", client.ClientId);
        Assert.Equal("client-secret", client.ClientSecret);
        Assert.Equal("https://snug.moe/oauth/authorize", client.AuthorizationEndpoint);
        Assert.Equal("https://snug.moe/oauth/token", client.TokenEndpoint);
        Assert.Equal(Mastodon.REQUESTED_SCOPES, client.Scopes);

        var registration = bed.Server.Requests.Single(r => r.Method == HttpMethod.Post);
        Assert.Contains("redirect_uris=" + Uri.EscapeDataString(MastodonTestBed.RedirectUri), registration.Body);
    }

    [Fact]
    public async Task The_secret_is_encrypted_at_rest()
    {
        bed.ServeInstance("snug.moe");
        bed.ServeAppRegistration("snug.moe");

        using (var db = bed.NewContext())
            await bed.NewRegistry(db).GetOrRegisterClientAsync(Snug);

        using var check = bed.NewContext();
        var stored = await check.OAuthClients.SingleAsync();

        Assert.NotEqual("client-secret", stored.EncryptedSecret);
        Assert.Equal("client-secret", bed.Protector.Unprotect(stored.EncryptedSecret));
    }

    [Fact]
    public async Task A_second_link_reuses_the_registration_without_touching_the_network()
    {
        bed.ServeInstance("snug.moe");
        bed.ServeAppRegistration("snug.moe");

        using (var db = bed.NewContext())
            await bed.NewRegistry(db).GetOrRegisterClientAsync(Snug);

        bed.Server.Requests.Clear();

        using var again = bed.NewContext();
        var client = await bed.NewRegistry(again).GetOrRegisterClientAsync(Snug);

        Assert.Equal("client-id", client.ClientId);
        Assert.Empty(bed.Server.Requests);
    }

    [Fact]
    public async Task A_host_that_doesnt_speak_the_mastodon_api_is_never_posted_to()
    {
        bed.ServeAppRegistration("example.com");

        using var db = bed.NewContext();
        var ex = await Assert.ThrowsAsync<RpcException>(
            () => bed.NewRegistry(db).GetOrRegisterClientAsync(new Uri("https://example.com/")));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
        Assert.DoesNotContain(bed.Server.Requests, r => r.Method == HttpMethod.Post);
    }

    [Fact]
    public async Task A_v2_only_server_still_counts()
    {
        bed.Server.OnJson(HttpMethod.Get, "https://new.example/api/v2/instance", """{"domain":"new.example","version":"4.5.0"}""");
        bed.ServeAppRegistration("new.example");

        using var db = bed.NewContext();
        var client = await bed.NewRegistry(db).GetOrRegisterClientAsync(new Uri("https://new.example/"));

        Assert.Equal("client-id", client.ClientId);
    }

    [Theory]
    [InlineData("https://evil.example/oauth/token")]
    [InlineData("http://snug.moe/oauth/token")]
    [InlineData("https://127.0.0.1/oauth/token")]
    [InlineData("https://snug.moe:9200/oauth/token")]
    public async Task Metadata_pointing_off_the_instance_is_refused_before_registering(string tokenEndpoint)
    {
        bed.ServeInstance("snug.moe");
        bed.ServeAppRegistration("snug.moe");
        bed.Server.OnJson(HttpMethod.Get, "https://snug.moe/.well-known/oauth-authorization-server", $$"""
            {
              "issuer": "https://snug.moe/",
              "authorization_endpoint": "https://snug.moe/oauth/authorize",
              "token_endpoint": "{{tokenEndpoint}}",
              "app_registration_endpoint": "https://snug.moe/api/v1/apps"
            }
            """);

        using var db = bed.NewContext();
        var ex = await Assert.ThrowsAsync<RpcException>(() => bed.NewRegistry(db).GetOrRegisterClientAsync(Snug));

        Assert.Equal(StatusCode.FailedPrecondition, ex.StatusCode);
        Assert.DoesNotContain(bed.Server.Requests, r => r.Method == HttpMethod.Post);
    }

    [Fact]
    public async Task A_registration_endpoint_off_the_instance_is_refused()
    {
        bed.ServeInstance("snug.moe");
        bed.ServeAppRegistration("evil.example");
        bed.Server.OnJson(HttpMethod.Get, "https://snug.moe/.well-known/oauth-authorization-server", """
            {
              "authorization_endpoint": "https://snug.moe/oauth/authorize",
              "token_endpoint": "https://snug.moe/oauth/token",
              "app_registration_endpoint": "https://evil.example/api/v1/apps"
            }
            """);

        using var db = bed.NewContext();
        await Assert.ThrowsAsync<RpcException>(() => bed.NewRegistry(db).GetOrRegisterClientAsync(Snug));

        Assert.Empty(bed.Server.RequestsTo("evil.example"));
    }

    [Fact]
    public async Task Metadata_on_the_instance_is_used()
    {
        bed.ServeInstance("mastodon.social", "4.4.0");
        bed.Server.OnJson(HttpMethod.Get, "https://mastodon.social/.well-known/oauth-authorization-server", """
            {
              "issuer": "https://mastodon.social/",
              "authorization_endpoint": "https://mastodon.social/oauth/authorize",
              "token_endpoint": "https://mastodon.social/oauth/token",
              "revocation_endpoint": "https://mastodon.social/oauth/revoke",
              "app_registration_endpoint": "https://mastodon.social/api/v1/apps",
              "scopes_supported": ["read", "write", "read:accounts", "read:statuses", "read:notifications", "write:statuses", "write:media", "profile"]
            }
            """);
        bed.ServeAppRegistration("mastodon.social");

        using var db = bed.NewContext();
        var client = await bed.NewRegistry(db).GetOrRegisterClientAsync(new Uri("https://mastodon.social/"));

        Assert.Equal("https://mastodon.social/oauth/revoke", client.RevocationEndpoint);
        Assert.Equal(Mastodon.REQUESTED_SCOPES, client.Scopes);
    }

    [Fact]
    public async Task Granular_scopes_the_server_rejects_fall_back_to_read_write()
    {
        bed.ServeInstance("old.example", "3.5.0");

        var attempts = 0;
        bed.Server.On(HttpMethod.Post, "https://old.example/api/v1/apps", () => ++attempts == 1
            ? FakeFediverseHandler.Json("""{"error":"Validation failed: Scopes are invalid"}""", HttpStatusCode.UnprocessableEntity)
            : FakeFediverseHandler.Json("""{"client_id":"old-id","client_secret":"old-secret"}"""));

        using var db = bed.NewContext();
        var client = await bed.NewRegistry(db).GetOrRegisterClientAsync(new Uri("https://old.example/"));

        Assert.Equal(Mastodon.FALLBACK_SCOPES, client.Scopes);
        Assert.Equal(2, attempts);
        Assert.Contains("scopes=read+write", bed.Server.Requests.Last().Body);
    }

    [Fact]
    public async Task Advertised_scopes_without_the_granular_ones_register_read_write_straight_away()
    {
        bed.ServeInstance("pleroma.example", "2.7.2 (compatible; Pleroma 2.6.0)");
        bed.Server.OnJson(HttpMethod.Get, "https://pleroma.example/.well-known/oauth-authorization-server",
            """{"scopes_supported":["read","write","follow","push"]}""");
        bed.ServeAppRegistration("pleroma.example");

        using var db = bed.NewContext();
        var client = await bed.NewRegistry(db).GetOrRegisterClientAsync(new Uri("https://pleroma.example/"));

        Assert.Equal(Mastodon.FALLBACK_SCOPES, client.Scopes);
        Assert.Single(bed.Server.Requests, r => r.Method == HttpMethod.Post);
    }

    [Fact]
    public async Task A_server_that_refuses_both_scope_sets_fails_the_link()
    {
        bed.ServeInstance("grumpy.example");
        bed.Server.OnJson(HttpMethod.Post, "https://grumpy.example/api/v1/apps", """{"error":"no"}""", HttpStatusCode.UnprocessableEntity);

        using var db = bed.NewContext();
        await Assert.ThrowsAsync<RpcException>(() => bed.NewRegistry(db).GetOrRegisterClientAsync(new Uri("https://grumpy.example/")));

        using var check = bed.NewContext();
        Assert.Empty(await check.OAuthClients.ToListAsync());
    }

    [Fact]
    public async Task A_changed_redirect_url_re_registers()
    {
        bed.ServeInstance("snug.moe");
        bed.ServeAppRegistration("snug.moe", clientId: "first");

        using (var db = bed.NewContext())
            await bed.NewRegistry(db).GetOrRegisterClientAsync(Snug);

        bed.Description.RedirectUri = "https://login.relivewp.net/oauth/callback/mastodon";
        bed.ServeAppRegistration("snug.moe", clientId: "second");

        using var again = bed.NewContext();
        var client = await bed.NewRegistry(again).GetOrRegisterClientAsync(Snug);

        Assert.Equal("second", client.ClientId);
        Assert.Equal("https://login.relivewp.net/oauth/callback/mastodon", client.RedirectUri);
    }

    [Fact]
    public async Task A_secret_about_to_expire_re_registers()
    {
        bed.ServeInstance("snug.moe");
        bed.ServeAppRegistration("snug.moe", clientId: "expiring", expiresAt: DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds());

        using (var db = bed.NewContext())
            await bed.NewRegistry(db).GetOrRegisterClientAsync(Snug);

        bed.ServeAppRegistration("snug.moe", clientId: "fresh");

        using var again = bed.NewContext();
        var client = await bed.NewRegistry(again).GetOrRegisterClientAsync(Snug);

        Assert.Equal("fresh", client.ClientId);
    }

    [Fact]
    public async Task Losing_the_registration_race_hands_back_the_winner()
    {
        bed.ServeInstance("snug.moe");
        bed.Server.On(HttpMethod.Post, "https://snug.moe/api/v1/apps", () =>
        {
            using var other = bed.NewContext();
            other.OAuthClients.Add(new LiveOAuthClient
            {
                Authority = "snug.moe",
                Service = Mastodon.SERVICE_NAME,
                ClientId = "winner",
                EncryptedSecret = bed.Protector.Protect("winner-secret"),
                RedirectUri = MastodonTestBed.RedirectUri,
                RequestedScopes = Mastodon.REQUESTED_SCOPES,
                RegisteredScopes = Mastodon.REQUESTED_SCOPES,
                AuthorizationEndpoint = "https://snug.moe/oauth/authorize",
                TokenEndpoint = "https://snug.moe/oauth/token",
                RegisteredAt = DateTimeOffset.UtcNow,
            });
            other.SaveChanges();

            return FakeFediverseHandler.Json("""{"client_id":"loser","client_secret":"loser-secret"}""");
        });

        using var db = bed.NewContext();
        var client = await bed.NewRegistry(db).GetOrRegisterClientAsync(Snug);

        Assert.Equal("winner", client.ClientId);
        Assert.Equal("winner-secret", client.ClientSecret);
    }

    [Theory]
    [InlineData("https://127.0.0.1/")]
    [InlineData("http://snug.moe/")]
    [InlineData("https://snug.moe/some/path")]
    [InlineData("https://snug.moe:8443/")]
    public async Task Only_https_instance_roots_are_accepted(string instance)
    {
        using var db = bed.NewContext();
        await Assert.ThrowsAsync<ArgumentException>(() => bed.NewRegistry(db).GetOrRegisterClientAsync(new Uri(instance)));

        Assert.Empty(bed.Server.Requests);
    }

    [Fact]
    public async Task Forgetting_a_client_removes_it()
    {
        bed.ServeInstance("snug.moe");
        bed.ServeAppRegistration("snug.moe");

        using var db = bed.NewContext();
        var registry = bed.NewRegistry(db);
        await registry.GetOrRegisterClientAsync(Snug);
        await registry.ForgetClientAsync(Snug);

        Assert.Null(await registry.FindClientAsync(Snug));
    }
}
