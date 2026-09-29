using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReLiveWP.Backend.Chat.Data;
using ReLiveWP.Backend.Chat.Services;
using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Backend.Chat.Tests;

public class PresenceServiceTests
{
    private const string Alice = "user-alice";
    private const string Bob = "user-bob";
    private const string AliceAddress = "alice@example.com";
    private const string BobAddress = "bob@example.com";

    private static readonly TimeSpan SessionTimeout = TimeSpan.FromDays(3);
    private static readonly TimeSpan ActiveWindow = TimeSpan.FromMinutes(5);

    private readonly InMemoryChatStateStore state = new();
    private readonly FakeAudienceSource audience = new();
    private readonly RecordingDeliveryQueue deliveries = new();
    private readonly ManualTimeProvider time = new(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
    private readonly ChatOptions chatOptions = new() { ActiveWindow = ActiveWindow };
    private readonly PresenceService presence;

    public PresenceServiceTests()
    {
        var options = Options.Create(chatOptions);
        presence = new PresenceService(
            state, audience, deliveries, new InMemoryStoredMessageStore(deliveries, chatOptions), new AlwaysAcquiredLockFactory(),
            options, time, NullLogger<PresenceService>.Instance);
    }

    private Task RegisterAsync(
        string endpointId, string userId, string address, PresenceStatus status = PresenceStatus.Online, string? device = null) =>
        presence.RegisterEndpointAsync(
            endpointId, userId, address, status, SessionTimeout, device ?? "device-" + endpointId, CancellationToken.None);

    private async Task BothOnlineAndMutualAsync()
    {
        audience.MakeMutual(Alice, Bob);
        await RegisterAsync("ep-alice", Alice, AliceAddress);
        await RegisterAsync("ep-bob", Bob, BobAddress);
        deliveries.Reset();
    }

    [Fact]
    public async Task Contacts_who_can_see_each_other_see_each_other_sign_in()
    {
        audience.MakeMutual(Alice, Bob);

        await RegisterAsync("ep-alice", Alice, AliceAddress);
        Assert.True(deliveries.IsEmpty);

        await RegisterAsync("ep-bob", Bob, BobAddress);

        Assert.Equal([(AliceAddress, PresenceStatus.Online)], deliveries.PresenceFor("ep-bob"));
        Assert.Equal([(BobAddress, PresenceStatus.Online)], deliveries.PresenceFor("ep-alice"));
    }

    [Fact]
    public async Task Visibility_is_one_way()
    {
        audience.LetSee(Alice, Bob);

        await RegisterAsync("ep-alice", Alice, AliceAddress);
        await RegisterAsync("ep-bob", Bob, BobAddress);

        Assert.Equal([(BobAddress, PresenceStatus.Online)], deliveries.PresenceFor("ep-alice"));
        Assert.Empty(deliveries.PresenceFor("ep-bob"));

        deliveries.Reset();
        await presence.SetPresenceAsync("ep-alice", PresenceStatus.Busy, CancellationToken.None);
        await presence.SetPresenceAsync("ep-bob", PresenceStatus.Away, CancellationToken.None);

        Assert.Equal([(BobAddress, PresenceStatus.Away)], deliveries.PresenceFor("ep-alice"));
        Assert.Empty(deliveries.PresenceFor("ep-bob"));
    }

    [Fact]
    public async Task Becoming_discoverable_shows_you_to_whoever_can_now_see_you()
    {
        await RegisterAsync("ep-alice", Alice, AliceAddress);
        await RegisterAsync("ep-bob", Bob, BobAddress);

        audience.LetSee(Alice, Bob);
        await presence.RefreshAudienceAsync(Bob, CancellationToken.None);

        Assert.Equal([(BobAddress, PresenceStatus.Online)], deliveries.PresenceFor("ep-alice"));
        Assert.Empty(deliveries.PresenceFor("ep-bob"));
        deliveries.Reset();

        audience.StopSeeing(Alice, Bob);
        await presence.RefreshAudienceAsync(Bob, CancellationToken.None);

        Assert.Equal([(BobAddress, PresenceStatus.Offline)], deliveries.PresenceFor("ep-alice"));
    }

    [Fact]
    public async Task Strangers_see_nothing()
    {
        await RegisterAsync("ep-alice", Alice, AliceAddress);
        await RegisterAsync("ep-bob", Bob, BobAddress);

        Assert.True(deliveries.IsEmpty);
    }

    [Fact]
    public async Task Hidden_looks_offline_until_it_comes_back()
    {
        audience.MakeMutual(Alice, Bob);
        await RegisterAsync("ep-bob", Bob, BobAddress);

        await RegisterAsync("ep-alice", Alice, AliceAddress, PresenceStatus.Hidden);
        Assert.Empty(deliveries.PresenceFor("ep-bob"));

        await presence.SetPresenceAsync("ep-alice", PresenceStatus.Online, CancellationToken.None);
        Assert.Equal([(AliceAddress, PresenceStatus.Online)], deliveries.PresenceFor("ep-bob"));
    }

    [Fact]
    public async Task Going_hidden_looks_like_signing_out()
    {
        await BothOnlineAndMutualAsync();

        await presence.SetPresenceAsync("ep-alice", PresenceStatus.Hidden, CancellationToken.None);

        Assert.Equal([(AliceAddress, PresenceStatus.Offline)], deliveries.PresenceFor("ep-bob"));
    }

    [Fact]
    public async Task A_quiet_online_endpoint_shows_idle_and_activity_brings_it_back()
    {
        await BothOnlineAndMutualAsync();

        time.Advance(TimeSpan.FromMinutes(4));
        await presence.ReportActivityAsync("ep-bob", CancellationToken.None);
        time.Advance(TimeSpan.FromMinutes(1));
        await presence.SweepAsync(CancellationToken.None);

        Assert.Equal([(AliceAddress, PresenceStatus.Idle)], deliveries.PresenceFor("ep-bob"));
        Assert.Empty(deliveries.PresenceFor("ep-alice"));

        await presence.ReportActivityAsync("ep-alice", CancellationToken.None);

        Assert.Equal(
            [(AliceAddress, PresenceStatus.Idle), (AliceAddress, PresenceStatus.Online)],
            deliveries.PresenceFor("ep-bob"));
    }

    [Fact]
    public async Task Busy_stays_busy_when_quiet()
    {
        await BothOnlineAndMutualAsync();
        await presence.SetPresenceAsync("ep-alice", PresenceStatus.Busy, CancellationToken.None);
        deliveries.Reset();

        time.Advance(ActiveWindow);
        await presence.SweepAsync(CancellationToken.None);

        Assert.DoesNotContain(deliveries.PresenceFor("ep-bob"), d => d.Address == AliceAddress);
    }

    [Fact]
    public async Task Idle_published_by_the_phone_is_passed_on()
    {
        await BothOnlineAndMutualAsync();

        await presence.SetPresenceAsync("ep-alice", PresenceStatus.Idle, CancellationToken.None);

        Assert.Equal([(AliceAddress, PresenceStatus.Idle)], deliveries.PresenceFor("ep-bob"));
    }

    [Fact]
    public async Task Ending_the_last_endpoint_shows_offline()
    {
        await BothOnlineAndMutualAsync();

        await presence.EndEndpointAsync("ep-alice", CancellationToken.None);

        Assert.Equal([(AliceAddress, PresenceStatus.Offline)], deliveries.PresenceFor("ep-bob"));
        Assert.Equal(["ep-alice"], deliveries.Cleared);
        Assert.False(state.HasEndpoint("ep-alice"));
    }

    [Fact]
    public async Task A_second_endpoint_keeps_the_user_online()
    {
        await BothOnlineAndMutualAsync();
        await RegisterAsync("ep-alice-2", Alice, AliceAddress);

        Assert.Equal([(BobAddress, PresenceStatus.Online)], deliveries.PresenceFor("ep-alice-2"));
        Assert.All(deliveries.PresenceFor("ep-bob"), p => Assert.Equal(PresenceStatus.Online, p.Status));

        await presence.EndEndpointAsync("ep-alice", CancellationToken.None);
        Assert.All(deliveries.PresenceFor("ep-bob"), p => Assert.Equal(PresenceStatus.Online, p.Status));

        deliveries.Reset();
        await presence.EndEndpointAsync("ep-alice-2", CancellationToken.None);
        Assert.Equal([(AliceAddress, PresenceStatus.Offline)], deliveries.PresenceFor("ep-bob"));
    }

    [Fact]
    public async Task An_endpoint_expires_after_its_session_timeout()
    {
        await BothOnlineAndMutualAsync();

        time.Advance(SessionTimeout - TimeSpan.FromMinutes(1));
        await presence.ReportActivityAsync("ep-bob", CancellationToken.None);
        time.Advance(TimeSpan.FromMinutes(1));
        await presence.SweepAsync(CancellationToken.None);

        Assert.Contains((AliceAddress, PresenceStatus.Offline), deliveries.PresenceFor("ep-bob"));
        Assert.False(state.HasEndpoint("ep-alice"));
        Assert.True(state.HasEndpoint("ep-bob"));
    }

    [Fact]
    public async Task An_expiry_that_fails_is_tried_again_and_does_not_hold_up_the_rest()
    {
        await BothOnlineAndMutualAsync();
        time.Advance(ActiveWindow);
        await presence.SweepAsync(CancellationToken.None);

        time.Advance(SessionTimeout);
        state.FailFinds("ep-alice", 1);
        await presence.SweepAsync(CancellationToken.None);

        Assert.True(state.HasEndpoint("ep-alice"));
        Assert.False(state.HasEndpoint("ep-bob"));

        await presence.SweepAsync(CancellationToken.None);

        Assert.False(state.HasEndpoint("ep-alice"));
    }

    [Fact]
    public async Task Becoming_mutual_exchanges_presence_and_breaking_it_withdraws_it()
    {
        await RegisterAsync("ep-alice", Alice, AliceAddress);
        await RegisterAsync("ep-bob", Bob, BobAddress);

        audience.MakeMutual(Alice, Bob);
        await presence.RefreshAudienceAsync(Alice, CancellationToken.None);

        Assert.Equal([(BobAddress, PresenceStatus.Online)], deliveries.PresenceFor("ep-alice"));
        Assert.Equal([(AliceAddress, PresenceStatus.Online)], deliveries.PresenceFor("ep-bob"));
        deliveries.Reset();

        audience.BreakMutual(Alice, Bob);
        await presence.RefreshAudienceAsync(Bob, CancellationToken.None);

        Assert.Equal([(BobAddress, PresenceStatus.Offline)], deliveries.PresenceFor("ep-alice"));
        Assert.Equal([(AliceAddress, PresenceStatus.Offline)], deliveries.PresenceFor("ep-bob"));
    }

    [Fact]
    public async Task A_refreshed_audience_is_used_by_the_other_side_too()
    {
        await RegisterAsync("ep-alice", Alice, AliceAddress);
        await RegisterAsync("ep-bob", Bob, BobAddress);
        audience.MakeMutual(Alice, Bob);
        await presence.RefreshAudienceAsync(Alice, CancellationToken.None);
        deliveries.Reset();

        await presence.SetPresenceAsync("ep-bob", PresenceStatus.Busy, CancellationToken.None);

        Assert.Equal([(BobAddress, PresenceStatus.Busy)], deliveries.PresenceFor("ep-alice"));
    }

    [Fact]
    public async Task Mailbox_being_down_at_sign_in_is_survivable()
    {
        audience.Unavailable = true;

        await RegisterAsync("ep-alice", Alice, AliceAddress);

        Assert.True(state.HasEndpoint("ep-alice"));
        Assert.True(deliveries.IsEmpty);
    }

    [Fact]
    public async Task Unknown_endpoints_are_reported_as_unknown()
    {
        Assert.Equal(EndpointState.Unknown, await presence.ReportActivityAsync("nope", CancellationToken.None));
        Assert.Equal(EndpointState.Unknown, await presence.SetPresenceAsync("nope", PresenceStatus.Busy, CancellationToken.None));
    }

    [Fact]
    public async Task Going_hidden_on_one_device_hides_you_everywhere()
    {
        await BothOnlineAndMutualAsync();
        await RegisterAsync("ep-alice-2", Alice, AliceAddress);
        deliveries.Reset();

        await presence.SetPresenceAsync("ep-alice-2", PresenceStatus.Hidden, CancellationToken.None);

        Assert.Equal([(AliceAddress, PresenceStatus.Offline)], deliveries.PresenceFor("ep-bob"));
    }

    [Fact]
    public async Task The_latest_choice_on_any_device_wins()
    {
        await BothOnlineAndMutualAsync();
        await RegisterAsync("ep-alice-2", Alice, AliceAddress);
        deliveries.Reset();

        await presence.SetPresenceAsync("ep-alice-2", PresenceStatus.Busy, CancellationToken.None);
        await presence.SetPresenceAsync("ep-alice", PresenceStatus.Away, CancellationToken.None);

        Assert.Equal(
            [(AliceAddress, PresenceStatus.Busy), (AliceAddress, PresenceStatus.Away)],
            deliveries.PresenceFor("ep-bob"));
    }

    [Fact]
    public async Task Signing_in_on_another_device_is_a_choice_too()
    {
        await BothOnlineAndMutualAsync();
        await presence.SetPresenceAsync("ep-alice", PresenceStatus.Hidden, CancellationToken.None);
        deliveries.Reset();

        await RegisterAsync("ep-alice-2", Alice, AliceAddress, PresenceStatus.Busy);

        Assert.Equal([(AliceAddress, PresenceStatus.Busy)], deliveries.PresenceFor("ep-bob"));
    }

    [Fact]
    public async Task A_phone_idling_on_the_charger_does_not_override_another_device()
    {
        await BothOnlineAndMutualAsync();
        await RegisterAsync("ep-alice-2", Alice, AliceAddress);
        deliveries.Reset();

        await presence.SetPresenceAsync("ep-alice-2", PresenceStatus.Idle, CancellationToken.None);
        Assert.Empty(deliveries.PresenceFor("ep-bob"));

        time.Advance(ActiveWindow);
        await presence.SweepAsync(CancellationToken.None);
        Assert.Equal([(AliceAddress, PresenceStatus.Idle)], deliveries.PresenceFor("ep-bob"));
    }

    [Fact]
    public async Task A_new_sign_in_from_the_same_device_replaces_the_old_one()
    {
        audience.MakeMutual(Alice, Bob);
        await RegisterAsync("ep-alice-old", Alice, AliceAddress, device: "alice-phone");
        await RegisterAsync("ep-bob", Bob, BobAddress);
        deliveries.Reset();

        await RegisterAsync("ep-alice-new", Alice, AliceAddress, PresenceStatus.Hidden, device: "alice-phone");

        Assert.Equal([(AliceAddress, PresenceStatus.Offline)], deliveries.PresenceFor("ep-bob"));
        Assert.False(state.HasEndpoint("ep-alice-old"));
        Assert.Empty(deliveries.PresenceFor("ep-alice-old"));
        Assert.Equal(EndpointState.Superseded, await presence.ReportActivityAsync("ep-alice-old", CancellationToken.None));
        Assert.Equal(EndpointState.Superseded, await presence.SetPresenceAsync("ep-alice-old", PresenceStatus.Online, CancellationToken.None));
    }

    [Fact]
    public async Task Replacing_an_endpoint_drops_the_old_device_session_for_contacts_without_a_status_change()
    {
        audience.MakeMutual(Alice, Bob);
        await RegisterAsync("ep-alice-old", Alice, AliceAddress, device: "alice-phone");
        await RegisterAsync("ep-bob", Bob, BobAddress);
        deliveries.Reset();

        await RegisterAsync("ep-alice-new", Alice, AliceAddress, device: "alice-phone");

        var change = Assert.Single(deliveries.PresenceChangesFor("ep-bob"));
        Assert.Equal(PresenceStatus.Online, change.Status);
        Assert.Equal(["alice-phone"], change.RemovedInstanceIds);
        Assert.Equal(["alice-phone"], change.InstanceIds);
        Assert.Equal([(BobAddress, PresenceStatus.Online)], deliveries.PresenceFor("ep-alice-new"));
    }

    [Fact]
    public async Task Presence_lists_every_device_the_contact_is_signed_in_on()
    {
        audience.MakeMutual(Alice, Bob);
        await RegisterAsync("ep-alice", Alice, AliceAddress, device: "alice-phone");
        await RegisterAsync("ep-alice-2", Alice, AliceAddress, device: "alice-other-phone");

        await RegisterAsync("ep-bob", Bob, BobAddress);

        var snapshot = Assert.Single(deliveries.PresenceChangesFor("ep-bob"));
        Assert.Equal(["alice-other-phone", "alice-phone"], snapshot.InstanceIds.Order());
    }

    [Fact]
    public async Task A_second_device_signing_in_tells_contacts_about_it()
    {
        audience.MakeMutual(Alice, Bob);
        await RegisterAsync("ep-alice", Alice, AliceAddress, device: "alice-phone");
        await RegisterAsync("ep-bob", Bob, BobAddress);
        deliveries.Reset();

        await RegisterAsync("ep-alice-2", Alice, AliceAddress, device: "alice-other-phone");

        var update = Assert.Single(deliveries.PresenceChangesFor("ep-bob"));
        Assert.Equal(PresenceStatus.Online, update.Status);
        Assert.Equal(["alice-other-phone", "alice-phone"], update.InstanceIds.Order());
        Assert.Empty(update.RemovedInstanceIds);
    }

    [Fact]
    public async Task A_device_signing_out_while_another_stays_is_removed_from_contacts()
    {
        audience.MakeMutual(Alice, Bob);
        await RegisterAsync("ep-alice", Alice, AliceAddress, device: "alice-phone");
        await RegisterAsync("ep-alice-2", Alice, AliceAddress, device: "alice-other-phone");
        await RegisterAsync("ep-bob", Bob, BobAddress);
        deliveries.Reset();

        await presence.EndEndpointAsync("ep-alice-2", CancellationToken.None);

        var update = Assert.Single(deliveries.PresenceChangesFor("ep-bob"));
        Assert.Equal(PresenceStatus.Online, update.Status);
        Assert.Equal(["alice-phone"], update.InstanceIds);
        Assert.Equal(["alice-other-phone"], update.RemovedInstanceIds);
    }

    [Fact]
    public async Task Offline_carries_no_device_list()
    {
        audience.MakeMutual(Alice, Bob);
        await RegisterAsync("ep-alice", Alice, AliceAddress, device: "alice-phone");
        await RegisterAsync("ep-bob", Bob, BobAddress);
        deliveries.Reset();

        await presence.EndEndpointAsync("ep-alice", CancellationToken.None);

        var update = Assert.Single(deliveries.PresenceChangesFor("ep-bob"));
        Assert.Equal(PresenceStatus.Offline, update.Status);
        Assert.Empty(update.InstanceIds);
        Assert.Empty(update.RemovedInstanceIds);
    }

    [Fact]
    public async Task Status_changes_past_the_allowance_reach_watchers_later_as_the_latest_status()
    {
        await BothOnlineAndMutualAsync();
        PresenceStatus[] flips = [PresenceStatus.Busy, PresenceStatus.Away, PresenceStatus.Busy, PresenceStatus.Away, PresenceStatus.Busy];
        foreach (var status in flips)
            await presence.SetPresenceAsync("ep-alice", status, CancellationToken.None);

        Assert.Equal(flips.Length, deliveries.PresenceFor("ep-bob").Count);
        deliveries.Reset();

        await presence.SetPresenceAsync("ep-alice", PresenceStatus.Online, CancellationToken.None);
        await presence.SetPresenceAsync("ep-alice", PresenceStatus.Away, CancellationToken.None);
        await presence.SweepAsync(CancellationToken.None);
        Assert.Empty(deliveries.PresenceFor("ep-bob"));

        time.Advance(chatOptions.PresenceRetryAfter);
        await presence.SweepAsync(CancellationToken.None);

        Assert.Equal([(AliceAddress, PresenceStatus.Away)], deliveries.PresenceFor("ep-bob"));
    }

    [Fact]
    public async Task A_deferred_status_is_what_a_watcher_signing_in_sees()
    {
        audience.MakeMutual(Alice, Bob);
        await RegisterAsync("ep-alice", Alice, AliceAddress);
        for (var i = 0; i < chatOptions.PresenceBurst; i++)
            await presence.SetPresenceAsync("ep-alice", i % 2 == 0 ? PresenceStatus.Busy : PresenceStatus.Online, CancellationToken.None);

        await presence.SetPresenceAsync("ep-alice", PresenceStatus.Away, CancellationToken.None);
        await RegisterAsync("ep-bob", Bob, BobAddress);
        Assert.Equal([(AliceAddress, PresenceStatus.Busy)], deliveries.PresenceFor("ep-bob"));

        time.Advance(chatOptions.PresenceRetryAfter);
        await presence.SweepAsync(CancellationToken.None);
        Assert.Equal(PresenceStatus.Away, deliveries.PresenceFor("ep-bob")[^1].Status);
    }

    [Fact]
    public async Task Signing_in_past_the_endpoint_cap_evicts_the_least_recently_active()
    {
        audience.MakeMutual(Alice, Bob);
        await RegisterAsync("ep-bob", Bob, BobAddress);
        for (var i = 0; i < chatOptions.MaxEndpointsPerUser; i++)
        {
            await RegisterAsync($"ep-alice-{i}", Alice, AliceAddress, device: $"alice-device-{i}");
            time.Advance(TimeSpan.FromSeconds(1));
        }

        deliveries.Reset();

        await RegisterAsync("ep-alice-new", Alice, AliceAddress, device: "alice-device-new");

        Assert.False(state.HasEndpoint("ep-alice-0"));
        Assert.True(state.HasEndpoint("ep-alice-1"));
        Assert.Equal(EndpointState.Evicted, await presence.ReportActivityAsync("ep-alice-0", CancellationToken.None));

        var update = Assert.Single(deliveries.PresenceChangesFor("ep-bob"));
        Assert.Equal(["alice-device-0"], update.RemovedInstanceIds);
        Assert.Equal(chatOptions.MaxEndpointsPerUser, update.InstanceIds.Count);
        Assert.DoesNotContain("alice-device-0", update.InstanceIds);
    }

    [Fact]
    public async Task An_endpoint_whose_queue_fills_up_is_evicted_on_the_next_sweep()
    {
        await BothOnlineAndMutualAsync();
        deliveries.MaxQueuedPerEndpoint = 1;

        await presence.SetPresenceAsync("ep-bob", PresenceStatus.Busy, CancellationToken.None);
        await presence.SetPresenceAsync("ep-bob", PresenceStatus.Away, CancellationToken.None);
        Assert.Equal([(BobAddress, PresenceStatus.Busy)], deliveries.PresenceFor("ep-alice"));

        await presence.SweepAsync(CancellationToken.None);

        Assert.False(state.HasEndpoint("ep-alice"));
        Assert.Contains("ep-alice", deliveries.Cleared);
        Assert.Equal([(AliceAddress, PresenceStatus.Offline)], deliveries.PresenceFor("ep-bob"));
        Assert.Equal(EndpointState.Evicted, await presence.SetPresenceAsync("ep-alice", PresenceStatus.Online, CancellationToken.None));
    }
}
