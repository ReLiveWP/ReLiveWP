using ReLiveWP.Backend.Chat.Data;
using ReLiveWP.Backend.Chat.Utilities;
using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Backend.Chat.Tests;

public class PresenceRulesTests
{
    private static ChatEndpoint Endpoint(PresenceStatus published = PresenceStatus.Online, bool active = true) =>
        new("ep", "user", "user@example.com", published, active, TimeSpan.FromDays(3), DateTimeOffset.UnixEpoch);

    [Theory]
    [InlineData(PresenceStatus.Online, PresenceStatus.Online)]
    [InlineData(PresenceStatus.Busy, PresenceStatus.Busy)]
    [InlineData(PresenceStatus.Away, PresenceStatus.Away)]
    [InlineData(PresenceStatus.Hidden, PresenceStatus.Offline)]
    public void The_chosen_status_is_what_shows(PresenceStatus chosen, PresenceStatus shown)
    {
        Assert.Equal(shown, PresenceRules.Show(chosen, [Endpoint()]));
    }

    [Fact]
    public void Online_shows_idle_when_no_device_is_present()
    {
        Assert.Equal(PresenceStatus.Idle, PresenceRules.Show(PresenceStatus.Online,
            [Endpoint(active: false), Endpoint(PresenceStatus.Idle)]));
    }

    [Fact]
    public void One_present_device_keeps_online()
    {
        Assert.Equal(PresenceStatus.Online, PresenceRules.Show(PresenceStatus.Online,
            [Endpoint(active: false), Endpoint(PresenceStatus.Idle), Endpoint()]));
    }

    [Fact]
    public void Busy_is_not_degraded_to_idle()
    {
        Assert.Equal(PresenceStatus.Busy, PresenceRules.Show(PresenceStatus.Busy, [Endpoint(active: false)]));
    }

    [Fact]
    public void No_endpoints_is_offline_whatever_was_chosen()
    {
        Assert.Equal(PresenceStatus.Offline, PresenceRules.Show(PresenceStatus.Busy, []));
    }

    [Theory]
    [InlineData(PresenceStatus.Online, true)]
    [InlineData(PresenceStatus.Hidden, true)]
    [InlineData(PresenceStatus.Busy, true)]
    [InlineData(PresenceStatus.Idle, false)]
    [InlineData(PresenceStatus.Offline, false)]
    public void Idle_from_the_phone_is_not_a_choice(PresenceStatus published, bool isChoice)
    {
        Assert.Equal(isChoice, PresenceRules.IsUserChoice(published));
    }
}
