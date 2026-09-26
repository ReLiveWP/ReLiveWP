using System.Net;
using System.Web;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Providers;
using ReLiveWP.Backend.ConnectedServices.Utilities;

using ServiceCaps = ReLiveWP.Backend.ConnectedServices.Data.LiveConnectedServiceCapabilities;

namespace ReLiveWP.Backend.ConnectedServices.Tests;

public class MastodonOAuthProviderTests : IDisposable
{
    private const string SnugActor = "https://snug.moe/users/8zlkadc6ao";

    // trimmed from a real snug.moe verify_credentials shape: Iceshrimp has no `uri`, but does send `fqn`
    private const string SnugAccount = """
        {"id":"8zlkadc6ao","username":"wamwoowam","acct":"wamwoowam","fqn":"wamwoowam@snug.moe",
         "display_name":"Wam","avatar":"https://snug.moe/files/thumbnail-427685b6"}
        """;

    private readonly MastodonTestBed bed = new();

    public MastodonOAuthProviderTests()
    {
        bed.ServeInstance("snug.moe");
        bed.ServeAppRegistration("snug.moe");
        bed.ServeWebFinger("snug.moe", "wamwoowam@snug.moe", SnugActor);
    }

    public void Dispose() => bed.Dispose();

    [Fact]
    public async Task Begin_builds_a_pkce_authorize_url_for_the_instance()
    {
        using var db = bed.NewContext();
        var pending = await bed.NewProvider(db).BeginAccountLinkAsync(Guid.NewGuid(), "@wamwoowam@snug.moe");

        var authorize = new Uri(pending.RedirectUri!);
        var query = HttpUtility.ParseQueryString(authorize.Query);

        Assert.Equal("https://snug.moe/oauth/authorize", authorize.GetLeftPart(UriPartial.Path));
        Assert.Equal("client-id", query["client_id"]);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal(MastodonTestBed.RedirectUri, query["redirect_uri"]);
        Assert.Equal(Mastodon.REQUESTED_SCOPES, query["scope"]);
        Assert.Equal(pending.State, query["state"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.Equal(PkceChallenge.CreateS256(pending.CodeVerifier!), query["code_challenge"]);

        Assert.Equal("https://snug.moe/", pending.Endpoint);
        Assert.Equal(Mastodon.SERVICE_NAME, pending.Service);
    }

    [Fact]
    public async Task Begin_never_leaks_the_client_secret_into_the_browser()
    {
        using var db = bed.NewContext();
        var pending = await bed.NewProvider(db).BeginAccountLinkAsync(Guid.NewGuid(), "@wamwoowam@snug.moe");

        Assert.DoesNotContain("client-secret", pending.RedirectUri);
    }

    [Fact]
    public async Task A_split_domain_handle_links_against_the_actors_host()
    {
        bed.ServeWebFinger("example.com", "wam@example.com", "https://social.example.com/users/wam");
        bed.ServeInstance("social.example.com", "4.4.0");
        bed.ServeAppRegistration("social.example.com");

        using var db = bed.NewContext();
        var pending = await bed.NewProvider(db).BeginAccountLinkAsync(Guid.NewGuid(), "wam@example.com");

        Assert.Equal("https://social.example.com/", pending.Endpoint);
        Assert.DoesNotContain(bed.Server.RequestsTo("example.com"), r => r.Method == HttpMethod.Post);
    }

    [Fact]
    public async Task A_webfinger_redirect_to_the_web_domain_is_followed()
    {
        bed.Server.On(HttpMethod.Get, "https://example.com/.well-known/webfinger?resource=acct%3Awam%40example.com",
            () => FakeFediverseHandler.Redirect("https://social.example.com/.well-known/webfinger?resource=acct%3Awam%40example.com"));
        bed.ServeWebFinger("social.example.com", "wam@example.com", "https://social.example.com/users/wam");
        bed.ServeInstance("social.example.com", "4.4.0");
        bed.ServeAppRegistration("social.example.com");

        using var db = bed.NewContext();
        var pending = await bed.NewProvider(db).BeginAccountLinkAsync(Guid.NewGuid(), "wam@example.com");

        Assert.Equal("https://social.example.com/", pending.Endpoint);
    }

    [Theory]
    [InlineData("http://social.example.com/.well-known/webfinger")]
    [InlineData("https://127.0.0.1/.well-known/webfinger")]
    [InlineData("https://localhost/.well-known/webfinger")]
    [InlineData("https://social.example.com:6379/")]
    public async Task A_webfinger_redirect_somewhere_unsafe_is_not_followed(string location)
    {
        bed.Server.On(HttpMethod.Get, "https://example.com/.well-known/webfinger?resource=acct%3Awam%40example.com",
            () => FakeFediverseHandler.Redirect(location));
        bed.ServeInstance("example.com");
        bed.ServeAppRegistration("example.com");

        using var db = bed.NewContext();
        var pending = await bed.NewProvider(db).BeginAccountLinkAsync(Guid.NewGuid(), "wam@example.com");

        Assert.Equal("https://example.com/", pending.Endpoint);
        Assert.DoesNotContain(bed.Server.Requests, r => r.Uri.Host != "example.com");
    }

    [Theory]
    [InlineData("https://127.0.0.1/users/wam")]
    [InlineData("http://social.example.com/users/wam")]
    [InlineData("https://social.example.com:8080/users/wam")]
    [InlineData("https://metadata.internal/users/wam")]
    public async Task A_webfinger_actor_that_isnt_a_public_https_host_is_ignored(string actor)
    {
        bed.ServeWebFinger("example.com", "wam@example.com", actor);
        bed.ServeInstance("example.com");
        bed.ServeAppRegistration("example.com");

        using var db = bed.NewContext();
        var pending = await bed.NewProvider(db).BeginAccountLinkAsync(Guid.NewGuid(), "wam@example.com");

        Assert.Equal("https://example.com/", pending.Endpoint);
    }

    [Fact]
    public async Task Begin_refuses_a_handle_it_cannot_parse()
    {
        using var db = bed.NewContext();
        var ex = await Assert.ThrowsAsync<RpcException>(
            () => bed.NewProvider(db).BeginAccountLinkAsync(Guid.NewGuid(), "wam@127.0.0.1"));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.Empty(bed.Server.Requests);
    }

    [Fact]
    public async Task Finalize_links_the_account_with_a_webfinger_actor_id()
    {
        ServeToken();
        bed.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/accounts/verify_credentials", SnugAccount);

        using var db = bed.NewContext();
        var provider = bed.NewProvider(db);
        var pending = await provider.BeginAccountLinkAsync(Guid.NewGuid(), "@wamwoowam@snug.moe");

        var service = await provider.FinalizeAccountLinkAsync(NewConnection(), pending, "the-code");

        Assert.Equal(Mastodon.SERVICE_NAME, service.Service);
        Assert.Equal("https://snug.moe/", service.ServiceUrl);
        Assert.Equal("the-token", service.AccessToken);
        Assert.Equal(DateTimeOffset.MaxValue, service.ExpiresAt);
        Assert.False(service.IsDueForRefresh);
        Assert.Equal(SnugActor, service.ServiceProfile.UserId);
        Assert.Equal("@wamwoowam@snug.moe", service.ServiceProfile.Username);
        Assert.Equal("Wam", service.ServiceProfile.DisplayName);
        Assert.Equal("https://snug.moe/files/thumbnail-427685b6", service.ServiceProfile.AvatarUrl);
        Assert.Equal(ServiceCaps.SocialFeed | ServiceCaps.SocialPhotos | ServiceCaps.SocialPost, service.AvailableCapabilities);

        var tokenRequest = bed.Server.Requests.Single(r => r.Uri.AbsolutePath == "/oauth/token");
        Assert.Contains("client_secret=client-secret", tokenRequest.Body);
        Assert.Contains("code_verifier=" + pending.CodeVerifier, tokenRequest.Body);

        var verify = bed.Server.Requests.Single(r => r.Uri.AbsolutePath == "/api/v1/accounts/verify_credentials");
        Assert.Equal("Bearer the-token", verify.Authorization);
    }

    // a missing token `scope` means `read` per the mastodon docs, iceshrimp enforces that and mastodon doesn't
    [Theory]
    [InlineData(Mastodon.REQUESTED_SCOPES)]
    [InlineData(Mastodon.FALLBACK_SCOPES)]
    public async Task Finalize_sends_the_authorized_scope_to_a_spec_strict_server(string registeredScopes)
    {
        if (registeredScopes == Mastodon.FALLBACK_SCOPES)
            bed.Server.OnJson(HttpMethod.Get, "https://snug.moe/.well-known/oauth-authorization-server",
                """{"scopes_supported":["read","write"]}""");

        using var db = bed.NewContext();
        var provider = bed.NewProvider(db);
        var pending = await provider.BeginAccountLinkAsync(Guid.NewGuid(), "@wamwoowam@snug.moe");

        var authorizedScope = HttpUtility.ParseQueryString(new Uri(pending.RedirectUri!).Query)["scope"];
        Assert.Equal(registeredScopes, authorizedScope);

        bed.Server.On(HttpMethod.Post, "https://snug.moe/oauth/token", () =>
        {
            var form = HttpUtility.ParseQueryString(bed.Server.Requests.Last().Body!);
            return (form["scope"] ?? "read") == authorizedScope
                ? FakeFediverseHandler.Json("""{"access_token":"the-token","token_type":"Bearer","created_at":1}""")
                : FakeFediverseHandler.Json("""{"error":"invalid_scope","error_description":"The requested scope is invalid, unknown, or malformed."}""", HttpStatusCode.BadRequest);
        });
        bed.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/accounts/verify_credentials", SnugAccount);

        var service = await provider.FinalizeAccountLinkAsync(NewConnection(), pending, "the-code");

        Assert.Equal("the-token", service.AccessToken);
        Assert.Equal(ServiceCaps.SocialFeed | ServiceCaps.SocialPhotos | ServiceCaps.SocialPost, service.AvailableCapabilities);
    }

    [Fact]
    public async Task Finalize_prefers_the_accounts_own_uri()
    {
        ServeToken();
        bed.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/accounts/verify_credentials",
            """{"id":"1","username":"wamwoowam","uri":"https://snug.moe/users/from-account"}""");

        var service = await LinkAsync();

        Assert.Equal("https://snug.moe/users/from-account", service.ServiceProfile.UserId);

        // the only webfinger is begin resolving the handle, finalize had no reason to ask
        Assert.Single(bed.Server.Requests, r => r.Uri.AbsolutePath == "/.well-known/webfinger");
    }

    [Theory]
    [InlineData("https://mastodon.social/users/Gargron")]
    [InlineData("https://snug.moe.evil.example/users/wam")]
    [InlineData("http://snug.moe/users/wam")]
    public async Task An_account_claiming_an_actor_off_the_instance_is_refused(string claimed)
    {
        ServeToken();
        bed.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/accounts/verify_credentials",
            $$"""{"id":"1","username":"wamwoowam","uri":"{{claimed}}"}""");

        await Assert.ThrowsAsync<RpcException>(LinkAsync);
    }

    [Fact]
    public async Task A_webfinger_actor_off_the_instance_is_refused_at_finalize()
    {
        ServeToken();
        bed.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/accounts/verify_credentials", SnugAccount);

        using var db = bed.NewContext();
        var provider = bed.NewProvider(db);
        var pending = await provider.BeginAccountLinkAsync(Guid.NewGuid(), "snug.moe");

        bed.ServeWebFinger("snug.moe", "wamwoowam@snug.moe", "https://mastodon.social/users/Gargron");

        await Assert.ThrowsAsync<RpcException>(() => provider.FinalizeAccountLinkAsync(NewConnection(), pending, "the-code"));
    }

    [Fact]
    public async Task A_non_https_avatar_is_dropped()
    {
        ServeToken();
        bed.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/accounts/verify_credentials",
            """{"id":"1","username":"wamwoowam","uri":"https://snug.moe/users/wam","avatar":"http://10.0.0.1/a.png"}""");

        var service = await LinkAsync();

        Assert.Null(service.ServiceProfile.AvatarUrl);
    }

    [Fact]
    public async Task An_fqn_for_someone_else_isnt_trusted_for_the_handle()
    {
        ServeToken();
        bed.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/accounts/verify_credentials",
            """{"id":"1","username":"wamwoowam","uri":"https://snug.moe/users/wam","fqn":"Gargron@mastodon.social"}""");

        var service = await LinkAsync();

        Assert.Equal("@wamwoowam@snug.moe", service.ServiceProfile.Username);
    }

    [Fact]
    public async Task A_read_write_grant_still_gets_every_capability()
    {
        bed.Server.OnJson(HttpMethod.Post, "https://snug.moe/oauth/token",
            """{"access_token":"the-token","token_type":"Bearer","scope":"read write","created_at":1}""");
        bed.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/accounts/verify_credentials", SnugAccount);

        var service = await LinkAsync();

        Assert.Equal(ServiceCaps.SocialFeed | ServiceCaps.SocialPhotos | ServiceCaps.SocialPost, service.AvailableCapabilities);
    }

    [Fact]
    public async Task An_invalid_client_forgets_the_registration()
    {
        bed.Server.OnJson(HttpMethod.Post, "https://snug.moe/oauth/token",
            """{"error":"invalid_client","error_description":"Client authentication failed"}""", HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<RpcException>(LinkAsync);

        using var check = bed.NewContext();
        Assert.Empty(await check.OAuthClients.ToListAsync());
    }

    [Fact]
    public async Task Finalize_refuses_a_pending_link_that_points_somewhere_unsafe()
    {
        using var db = bed.NewContext();
        var pending = new LivePendingOAuth
        {
            State = "s",
            UserId = Guid.NewGuid(),
            Service = Mastodon.SERVICE_NAME,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            Endpoint = "https://169.254.169.254/",
            CodeVerifier = "v",
        };

        await Assert.ThrowsAsync<RpcException>(() => bed.NewProvider(db).FinalizeAccountLinkAsync(NewConnection(), pending, "c"));
        Assert.Empty(bed.Server.Requests);
    }

    [Fact]
    public async Task Refresh_busts_a_revoked_token()
    {
        bed.Server.On(HttpMethod.Get, "https://snug.moe/api/v1/accounts/verify_credentials",
            () => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        using var db = bed.NewContext();
        Assert.False(await bed.NewProvider(db).RefreshTokensAsync(LinkedConnection()));
    }

    [Fact]
    public async Task Refresh_leaves_a_flaky_instance_linked()
    {
        bed.Server.On(HttpMethod.Get, "https://snug.moe/api/v1/accounts/verify_credentials",
            () => new HttpResponseMessage(HttpStatusCode.BadGateway));

        using var db = bed.NewContext();
        Assert.True(await bed.NewProvider(db).RefreshTokensAsync(LinkedConnection()));
    }

    [Fact]
    public async Task Refresh_updates_the_profile_but_not_the_identity()
    {
        bed.Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/accounts/verify_credentials",
            """{"id":"1","username":"wamwoowam","display_name":"Renamed","uri":"https://snug.moe/users/someone-else"}""");

        var connection = LinkedConnection();

        using var db = bed.NewContext();
        Assert.True(await bed.NewProvider(db).RefreshTokensAsync(connection));

        Assert.Equal("Renamed", connection.ServiceProfile.DisplayName);
        Assert.Equal(SnugActor, connection.ServiceProfile.UserId);
    }

    [Fact]
    public async Task Refresh_on_a_connection_with_an_unsafe_url_does_nothing()
    {
        var connection = LinkedConnection();
        connection.ServiceUrl = "https://127.0.0.1/";

        using var db = bed.NewContext();
        Assert.False(await bed.NewProvider(db).RefreshTokensAsync(connection));
        Assert.Empty(bed.Server.Requests);
    }

    [Fact]
    public async Task Unlinking_revokes_the_token_with_the_registered_client()
    {
        bed.Server.OnJson(HttpMethod.Post, "https://snug.moe/oauth/revoke", "{}");

        using var db = bed.NewContext();
        await bed.NewRegistry(db).GetOrRegisterClientAsync(new Uri("https://snug.moe/"));
        bed.Server.Requests.Clear();
        bed.HttpClientFactory.RequestedNames.Clear();

        await bed.NewProvider(db).RevokeTokensAsync(LinkedConnection());

        var revoke = Assert.Single(bed.Server.Requests);
        Assert.Equal("https://snug.moe/oauth/revoke", revoke.Uri.AbsoluteUri);
        Assert.Contains("token=the-token", revoke.Body);
        Assert.Contains("client_id=client-id", revoke.Body);
        Assert.Contains("client_secret=client-secret", revoke.Body);
        Assert.All(bed.HttpClientFactory.RequestedNames, name => Assert.Equal("GuardedOutbound", name));
    }

    [Fact]
    public async Task Unlinking_without_a_registration_sends_nothing()
    {
        using var db = bed.NewContext();
        await bed.NewProvider(db).RevokeTokensAsync(LinkedConnection());

        Assert.Empty(bed.Server.Requests);
    }

    [Fact]
    public async Task Unlinking_a_connection_with_an_unsafe_url_sends_nothing()
    {
        var connection = LinkedConnection();
        connection.ServiceUrl = "https://10.0.0.1/";

        using var db = bed.NewContext();
        await bed.NewProvider(db).RevokeTokensAsync(connection);

        Assert.Empty(bed.Server.Requests);
    }

    private void ServeToken()
        => bed.Server.OnJson(HttpMethod.Post, "https://snug.moe/oauth/token",
            $$"""{"access_token":"the-token","token_type":"Bearer","scope":"{{Mastodon.REQUESTED_SCOPES}}","created_at":1}""");

    private async Task<LiveConnectedService> LinkAsync()
    {
        using var db = bed.NewContext();
        var provider = bed.NewProvider(db);
        var pending = await provider.BeginAccountLinkAsync(Guid.NewGuid(), "@wamwoowam@snug.moe");

        return await provider.FinalizeAccountLinkAsync(NewConnection(), pending, "the-code");
    }

    private static LiveConnectedService NewConnection() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Service = default!,
        AccessToken = default!,
        RefreshToken = default!,
        ExpiresAt = default,
        Flags = LiveConnectedServiceFlags.None,
        AvailableCapabilities = ServiceCaps.SocialFeed | ServiceCaps.SocialPost | ServiceCaps.SocialPhotos,
        EnabledCapabilities = 0,
    };

    private static LiveConnectedService LinkedConnection()
    {
        var connection = NewConnection();
        connection.Service = Mastodon.SERVICE_NAME;
        connection.ServiceUrl = "https://snug.moe/";
        connection.AccessToken = "the-token";
        connection.RefreshToken = "";
        connection.ExpiresAt = DateTimeOffset.MaxValue;
        connection.ServiceProfile.UserId = SnugActor;
        return connection;
    }
}
