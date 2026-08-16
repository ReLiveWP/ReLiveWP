using ReLiveWP.Backend.ConnectedServices.OAuthProviders;

using ServiceCaps = ReLiveWP.Backend.ConnectedServices.Data.LiveConnectedServiceCapabilities;

namespace ReLiveWP.Backend.ConnectedServices.Tests;

public class GoogleScopeCapabilityTests
{
    private static ServiceCaps Caps(params string[] scopes) =>
        GoogleOAuthProvider.GetCapabilitiesFromScopes(
            [.. scopes.Select(s => $"https://www.googleapis.com/auth/{s}")]);

    // the family is the first segment, so every qualifier Google appends has to land on the same
    // capability. sync is read-only, so a full grant and a readonly one mean the same thing to us.
    [Theory]
    [InlineData("contacts.readonly")]
    [InlineData("contacts.other.readonly")]
    [InlineData("contacts")]
    public void Every_contacts_scope_grants_contacts(string scope)
    {
        Assert.True(Caps(scope).HasFlag(ServiceCaps.Contacts));
    }

    [Theory]
    [InlineData("calendar.readonly")]
    [InlineData("calendar.events.readonly")]
    [InlineData("calendar")]
    [InlineData("calendar.events")]
    public void Every_calendar_scope_grants_calendar(string scope)
    {
        Assert.True(Caps(scope).HasFlag(ServiceCaps.Calendar));
    }

    [Fact]
    public void Several_grants_on_one_connection_combine()
    {
        var caps = Caps("contacts.readonly", "calendar.readonly");

        Assert.True(caps.HasFlag(ServiceCaps.Contacts));
        Assert.True(caps.HasFlag(ServiceCaps.Calendar));
    }

    [Fact]
    public void The_other_families_still_map()
    {
        var caps = Caps("drive", "photoslibrary.appendonly", "gmail.modify");

        Assert.True(caps.HasFlag(ServiceCaps.FileStorage));
        Assert.True(caps.HasFlag(ServiceCaps.PhotoSync));
        Assert.True(caps.HasFlag(ServiceCaps.Email));
    }

    [Fact]
    public void Scopes_we_do_not_model_grant_nothing()
    {
        Assert.Equal((ServiceCaps)0, Caps("userinfo.profile", "userinfo.email"));
        Assert.Equal((ServiceCaps)0, GoogleOAuthProvider.GetCapabilitiesFromScopes(["openid"]));
    }
}
