using Microsoft.Extensions.Configuration;
using ReLiveWP.Services.Login.Models.Sso;

namespace ReLiveWP.Services.Login.Tests;

public class SsoClientRegistryTests
{
    private const string MailClientId = "548b5435-3d80-474c-81be-6fa4a5003471";

    private static SsoOptions Options()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"Sso:Clients:{MailClientId}:Name"] = "mail",
                [$"Sso:Clients:{MailClientId}:RedirectUris:0"] = "https://mail.relivewp.net/auth/callback",
                [$"Sso:Clients:{MailClientId}:PostLogoutRedirectUris:0"] = "https://mail.relivewp.net/",
                [$"Sso:Clients:{MailClientId}:ServiceTargets:0"] = "relivewp.net",
                [$"Sso:Clients:{MailClientId}:ServiceTargets:1"] = "sync.relivewp.net",
                [$"Sso:Clients:{MailClientId}:RequirePkce"] = "true",
            })
            .Build();

        var options = new SsoOptions();
        configuration.GetSection(SsoOptions.SectionName).Bind(options);

        return options;
    }

    [Fact]
    public void Binds_a_client_from_ini_style_indexed_keys()
    {
        var client = Options().FindClient(MailClientId);

        Assert.NotNull(client);
        Assert.True(client!.RequirePkce);
        Assert.Equal(2, client.ServiceTargets.Count);
        Assert.Equal("mail", client.Name);
    }

    [Fact]
    public void Unknown_client_is_not_found()
    {
        Assert.Null(Options().FindClient(Guid.NewGuid().ToString()));
        Assert.Null(Options().FindClient(null));
        Assert.Null(Options().FindClient(""));
    }

    [Fact]
    public void The_friendly_name_is_not_a_client_id()
    {
        Assert.Null(Options().FindClient("mail"));
    }

    [Fact]
    public void Client_lookup_ignores_case()
    {
        Assert.NotNull(Options().FindClient(MailClientId.ToUpperInvariant()));
    }

    [Theory]
    [InlineData("https://mail.relivewp.net/auth/callback", true)]
    [InlineData("https://mail.relivewp.net/auth/callback/", false)]
    [InlineData("https://mail.relivewp.net/auth/Callback", false)]
    [InlineData("https://evil.example/auth/callback", false)]
    [InlineData("https://mail.relivewp.net.evil.example/auth/callback", false)]
    [InlineData("https://mail.relivewp.net/auth/callback?next=x", false)]
    [InlineData("", false)]
    public void Redirect_uris_are_matched_exactly(string candidate, bool allowed)
    {
        Assert.Equal(allowed, Options().FindClient(MailClientId)!.IsRedirectUriAllowed(candidate));
    }

    [Fact]
    public void Post_logout_redirect_uris_are_a_separate_allowlist()
    {
        var client = Options().FindClient(MailClientId)!;

        Assert.True(client.IsPostLogoutRedirectUriAllowed("https://mail.relivewp.net/"));
        Assert.False(client.IsPostLogoutRedirectUriAllowed("https://evil.example/"));

        // a valid sign-in redirect is not automatically a valid sign-out redirect
        Assert.False(client.IsPostLogoutRedirectUriAllowed("https://mail.relivewp.net/auth/callback"));
    }

    [Fact]
    public void Requested_targets_must_be_a_subset_of_the_client_registration()
    {
        var client = Options().FindClient(MailClientId)!;

        Assert.True(client.AreTargetsAllowed(["relivewp.net"]));
        Assert.True(client.AreTargetsAllowed(["relivewp.net", "sync.relivewp.net"]));
        Assert.False(client.AreTargetsAllowed(["docs.relivewp.net"]));
        Assert.False(client.AreTargetsAllowed(["relivewp.net", "docs.relivewp.net"]));
    }

    [Fact]
    public void Target_matching_is_case_sensitive()
    {
        Assert.False(Options().FindClient(MailClientId)!.AreTargetsAllowed(["RELIVEWP.NET"]));
    }
}
