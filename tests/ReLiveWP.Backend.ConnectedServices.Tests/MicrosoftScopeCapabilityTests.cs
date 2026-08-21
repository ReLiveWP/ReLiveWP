using ReLiveWP.Backend.ConnectedServices.OAuthProviders;

using ServiceCaps = ReLiveWP.Backend.ConnectedServices.Data.LiveConnectedServiceCapabilities;

namespace ReLiveWP.Backend.ConnectedServices.Tests;

public class MicrosoftScopeCapabilityTests
{
    private static ServiceCaps Caps(params string[] scopes) =>
        MicrosoftOAuthProvider.GetCapabilitiesFromScopes(
            [.. scopes.Select(s => $"https://graph.microsoft.com/{s}")]);

    // the family is the first segment, so every qualifier Graph appends lands on one capability
    [Theory]
    [InlineData("Contacts.Read")]
    [InlineData("Contacts.ReadWrite")]
    public void Every_contacts_scope_grants_contacts(string scope)
        => Assert.True(Caps(scope).HasFlag(ServiceCaps.Contacts));

    // OneDrive backs both the document store and the camera roll off the same grant
    [Theory]
    [InlineData("Files.Read")]
    [InlineData("Files.ReadWrite")]
    [InlineData("Files.ReadWrite.All")]
    public void Every_files_scope_grants_storage_and_photos(string scope)
    {
        var caps = Caps(scope);

        Assert.True(caps.HasFlag(ServiceCaps.FileStorage));
        Assert.True(caps.HasFlag(ServiceCaps.PhotoSync));
    }

    [Fact]
    public void Several_grants_on_one_connection_combine()
    {
        var caps = Caps("Files.ReadWrite", "Contacts.Read");

        Assert.True(caps.HasFlag(ServiceCaps.FileStorage));
        Assert.True(caps.HasFlag(ServiceCaps.PhotoSync));
        Assert.True(caps.HasFlag(ServiceCaps.Contacts));
    }

    // a connection linked before Contacts.Read was requested must not claim contacts, or the mirror
    // pulls against a token that cannot serve it
    [Fact]
    public void A_files_only_grant_does_not_imply_contacts()
        => Assert.False(Caps("Files.ReadWrite", "User.Read").HasFlag(ServiceCaps.Contacts));

    [Fact]
    public void Scopes_we_do_not_model_grant_nothing()
    {
        Assert.Equal((ServiceCaps)0, Caps("User.Read"));
        Assert.Equal((ServiceCaps)0, MicrosoftOAuthProvider.GetCapabilitiesFromScopes(
            ["openid", "profile", "email", "offline_access"]));
    }
}
