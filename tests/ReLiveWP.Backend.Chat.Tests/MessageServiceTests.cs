using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReLiveWP.Backend.Chat.Services;
using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Backend.Chat.Tests;

public class MessageServiceTests
{
    private const string Alice = "user-alice";
    private const string Bob = "user-bob";
    private const string AliceAddress = "alice@example.com";
    private const string BobAddress = "bob@example.com";
    private const string Carol = "user-carol";
    private const string CarolAddress = "carol@example.com";

    private static readonly TimeSpan SessionTimeout = TimeSpan.FromDays(3);

    private readonly InMemoryChatStateStore state = new();
    private readonly FakeAudienceSource audience = new();
    private readonly RecordingDeliveryQueue deliveries = new();
    private readonly ManualTimeProvider time = new(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
    private readonly FakeUserDirectory directory = new();
    private readonly ChatOptions chatOptions = new()
    {
        MessageBurst = 3,
        MessageRefillPerSecond = 1,
        DataBurstBytes = 100,
        DataRefillBytesPerSecond = 10,
        MaxStoredPerRecipient = 3,
        MaxStoredPerPair = 2,
    };
    private readonly InMemoryStoredMessageStore stored;
    private readonly PresenceService presence;
    private readonly MessageService messages;

    public MessageServiceTests()
    {
        var options = Options.Create(chatOptions);
        stored = new InMemoryStoredMessageStore(deliveries, chatOptions);
        presence = new PresenceService(
            state, audience, deliveries, stored, new AlwaysAcquiredLockFactory(), options, time, NullLogger<PresenceService>.Instance);
        messages = new MessageService(
            state, deliveries, stored, directory, presence, options, time, NullLogger<MessageService>.Instance);

        directory.Add(AliceAddress, Alice);
        directory.Add(BobAddress, Bob);
        directory.Add(CarolAddress, Carol);
    }

    private Task RegisterAsync(string endpointId, string userId, string address, PresenceStatus status = PresenceStatus.Online) =>
        presence.RegisterEndpointAsync(
            endpointId, userId, address, status, SessionTimeout, "device-" + endpointId, CancellationToken.None);

    private Task<SendOutcome> SendAsync(
        string endpointId, string recipient, string text, string transactionId = "12", MessageKind kind = MessageKind.Text,
        string instanceId = "", bool storeIfOffline = false) =>
        messages.SendAsync(
            endpointId, transactionId, recipient, instanceId, kind, ByteString.CopyFromUtf8(text), storeIfOffline, CancellationToken.None);

    private Task<SendOutcome> SendOfflineAsync(string endpointId, string recipient, string text, string transactionId = "12") =>
        SendAsync(endpointId, recipient, text, transactionId, storeIfOffline: true);

    private async Task BobSignedOutAsync()
    {
        await RegisterAsync("ep-bob", Bob, BobAddress);
        await presence.EndEndpointAsync("ep-bob", CancellationToken.None);
        deliveries.Reset();
    }

    private async Task BothOnlineAsync()
    {
        await RegisterAsync("ep-alice", Alice, AliceAddress);
        await RegisterAsync("ep-bob", Bob, BobAddress);
        deliveries.Reset();
    }

    [Fact]
    public async Task A_message_reaches_every_endpoint_of_the_recipient()
    {
        audience.MakeMutual(Alice, Bob);
        await BothOnlineAsync();
        await RegisterAsync("ep-bob-2", Bob, BobAddress);
        deliveries.Reset();

        var outcome = await SendAsync("ep-alice", BobAddress, "hello");

        Assert.Equal(SendOutcome.Delivered, outcome);
        Assert.Equal(["hello"], deliveries.MessagesFor("ep-bob"));
        Assert.Equal(["hello"], deliveries.MessagesFor("ep-bob-2"));
        Assert.Empty(deliveries.MessagesFor("ep-alice"));
    }

    [Fact]
    public async Task The_sender_gets_a_copy_on_their_other_endpoints()
    {
        audience.MakeMutual(Alice, Bob);
        await BothOnlineAsync();
        await RegisterAsync("ep-alice-2", Alice, AliceAddress);
        deliveries.Reset();

        await SendAsync("ep-alice", BobAddress, "hello");

        Assert.Equal(["hello"], deliveries.MessagesFor("ep-alice-2"));
        Assert.Empty(deliveries.MessagesFor("ep-alice"));
    }

    [Fact]
    public async Task The_recipient_address_is_matched_without_case()
    {
        audience.MakeMutual(Alice, Bob);
        await BothOnlineAsync();

        var outcome = await SendAsync("ep-alice", "Bob@Example.COM", "hello");

        Assert.Equal(SendOutcome.Delivered, outcome);
        Assert.Equal(["hello"], deliveries.MessagesFor("ep-bob"));
    }

    [Fact]
    public async Task Either_side_seeing_the_other_is_enough()
    {
        audience.LetSee(Alice, Bob);
        await BothOnlineAsync();

        var aliceToBob = await SendAsync("ep-alice", BobAddress, "hi bob");
        var bobToAlice = await SendAsync("ep-bob", AliceAddress, "hi alice");

        Assert.Equal(SendOutcome.Delivered, aliceToBob);
        Assert.Equal(SendOutcome.Delivered, bobToAlice);
        Assert.Equal(["hi bob"], deliveries.MessagesFor("ep-bob"));
        Assert.Equal(["hi alice"], deliveries.MessagesFor("ep-alice"));
    }

    [Fact]
    public async Task Replying_does_not_leak_presence_the_other_way()
    {
        audience.LetSee(Alice, Bob);
        await BothOnlineAsync();

        await SendAsync("ep-bob", AliceAddress, "hi alice");

        Assert.Empty(deliveries.PresenceFor("ep-bob"));
    }

    [Fact]
    public async Task Strangers_are_voided_but_their_own_endpoints_still_see_it_sent()
    {
        await BothOnlineAsync();
        await RegisterAsync("ep-alice-2", Alice, AliceAddress);
        deliveries.Reset();

        var outcome = await SendAsync("ep-alice", BobAddress, "hello?");

        Assert.Equal(SendOutcome.Voided, outcome);
        Assert.Empty(deliveries.MessagesFor("ep-bob"));
        Assert.Equal(["hello?"], deliveries.MessagesFor("ep-alice-2"));
    }

    [Fact]
    public async Task A_hidden_recipient_still_gets_messages()
    {
        audience.MakeMutual(Alice, Bob);
        await RegisterAsync("ep-alice", Alice, AliceAddress);
        await RegisterAsync("ep-bob", Bob, BobAddress, PresenceStatus.Hidden);
        deliveries.Reset();

        var outcome = await SendAsync("ep-alice", BobAddress, "you there?");

        Assert.Equal(SendOutcome.Delivered, outcome);
        Assert.Equal(["you there?"], deliveries.MessagesFor("ep-bob"));
    }

    [Fact]
    public async Task A_recipient_with_no_endpoints_is_offline_and_nothing_is_echoed()
    {
        audience.MakeMutual(Alice, Bob);
        await RegisterAsync("ep-alice", Alice, AliceAddress);
        await RegisterAsync("ep-alice-2", Alice, AliceAddress);
        await RegisterAsync("ep-bob", Bob, BobAddress);
        await presence.EndEndpointAsync("ep-bob", CancellationToken.None);
        deliveries.Reset();

        var outcome = await SendAsync("ep-alice", BobAddress, "hello");

        Assert.Equal(SendOutcome.RecipientOffline, outcome);
        Assert.Empty(deliveries.MessagesFor("ep-alice-2"));
    }

    [Fact]
    public async Task An_address_nobody_has_is_voided()
    {
        await RegisterAsync("ep-alice", Alice, AliceAddress);

        var online = await SendAsync("ep-alice", "nobody@example.com", "hello");
        var offline = await SendOfflineAsync("ep-alice", "nobody@example.com", "hello", transactionId: "13");

        Assert.Equal(SendOutcome.Voided, online);
        Assert.Equal(SendOutcome.Voided, offline);
    }

    [Fact]
    public async Task An_offline_message_is_stored_and_echoed_but_not_delivered()
    {
        audience.MakeMutual(Alice, Bob);
        await BobSignedOutAsync();
        await RegisterAsync("ep-alice", Alice, AliceAddress);
        await RegisterAsync("ep-alice-2", Alice, AliceAddress);
        deliveries.Reset();

        var outcome = await SendOfflineAsync("ep-alice", BobAddress, "see you later");

        Assert.Equal(SendOutcome.Stored, outcome);
        Assert.Equal(["see you later"], stored.WaitingFor(Bob));
        Assert.Equal(["see you later"], deliveries.MessagesFor("ep-alice-2"));
        Assert.Equal(0, deliveries.ReceivedFor("ep-alice-2").Single().OriginalArrivalUnixMs);
    }

    [Fact]
    public async Task Stored_messages_arrive_at_the_next_sign_in_after_presence_with_their_send_time()
    {
        audience.MakeMutual(Alice, Bob);
        await BobSignedOutAsync();
        await RegisterAsync("ep-alice", Alice, AliceAddress);
        var sentAt = time.GetUtcNow();
        await SendOfflineAsync("ep-alice", BobAddress, "first", transactionId: "1");
        time.Advance(TimeSpan.FromMinutes(1));
        await SendOfflineAsync("ep-alice", BobAddress, "second", transactionId: "2");
        time.Advance(TimeSpan.FromHours(5));
        deliveries.Reset();

        await RegisterAsync("ep-bob-new", Bob, BobAddress);

        var received = deliveries.ReceivedFor("ep-bob-new");
        Assert.Equal(["first", "second"], received.Select(r => r.Payload.ToStringUtf8()));
        Assert.Equal(sentAt.ToUnixTimeMilliseconds(), received[0].OriginalArrivalUnixMs);
        Assert.Equal(sentAt.AddMinutes(1).ToUnixTimeMilliseconds(), received[1].OriginalArrivalUnixMs);
        Assert.Equal([(AliceAddress, PresenceStatus.Online)], deliveries.PresenceFor("ep-bob-new"));
        Assert.Empty(stored.WaitingFor(Bob));
    }

    [Fact]
    public async Task Stored_messages_go_to_the_first_sign_in_only()
    {
        audience.MakeMutual(Alice, Bob);
        await BobSignedOutAsync();
        await RegisterAsync("ep-alice", Alice, AliceAddress);
        await SendOfflineAsync("ep-alice", BobAddress, "hello");

        await RegisterAsync("ep-bob-phone", Bob, BobAddress);
        await RegisterAsync("ep-bob-tablet", Bob, BobAddress);

        Assert.Equal(["hello"], deliveries.MessagesFor("ep-bob-phone"));
        Assert.Empty(deliveries.MessagesFor("ep-bob-tablet"));
    }

    [Fact]
    public async Task A_gated_offline_recipient_looks_the_same_as_any_other_void()
    {
        await BobSignedOutAsync();
        await RegisterAsync("ep-alice", Alice, AliceAddress);

        var online = await SendAsync("ep-alice", BobAddress, "hello", transactionId: "1");
        var offline = await SendOfflineAsync("ep-alice", BobAddress, "hello", transactionId: "2");

        Assert.Equal(SendOutcome.Voided, online);
        Assert.Equal(SendOutcome.Voided, offline);
        Assert.Empty(stored.WaitingFor(Bob));
    }

    [Fact]
    public async Task Data_is_never_stored()
    {
        audience.MakeMutual(Alice, Bob);
        await BobSignedOutAsync();
        await RegisterAsync("ep-alice", Alice, AliceAddress);

        var outcome = await SendAsync("ep-alice", BobAddress, "p2p", kind: MessageKind.Data, instanceId: "device-ep-bob", storeIfOffline: true);

        Assert.Equal(SendOutcome.RecipientOffline, outcome);
        Assert.Empty(stored.WaitingFor(Bob));
    }

    [Fact]
    public async Task One_sender_can_only_fill_part_of_a_mailbox()
    {
        audience.MakeMutual(Alice, Bob);
        audience.MakeMutual(Carol, Bob);
        await BobSignedOutAsync();
        await RegisterAsync("ep-alice", Alice, AliceAddress);
        await RegisterAsync("ep-carol", Carol, CarolAddress);

        Assert.Equal(SendOutcome.Stored, await SendOfflineAsync("ep-alice", BobAddress, "1", "1"));
        Assert.Equal(SendOutcome.Stored, await SendOfflineAsync("ep-alice", BobAddress, "2", "2"));
        Assert.Equal(SendOutcome.Throttled, await SendOfflineAsync("ep-alice", BobAddress, "3", "3"));
        Assert.Equal(SendOutcome.Stored, await SendOfflineAsync("ep-carol", BobAddress, "4", "4"));
        Assert.Equal(SendOutcome.Throttled, await SendOfflineAsync("ep-carol", BobAddress, "5", "5"));

        Assert.Equal(["1", "2", "4"], stored.WaitingFor(Bob));
    }

    [Fact]
    public async Task Identity_being_down_fails_a_send_to_an_offline_recipient_and_it_can_be_retried()
    {
        audience.MakeMutual(Alice, Bob);
        await BobSignedOutAsync();
        await RegisterAsync("ep-alice", Alice, AliceAddress);
        directory.Unavailable = true;

        var failed = await SendOfflineAsync("ep-alice", BobAddress, "hello", "9");
        directory.Unavailable = false;
        var retried = await SendOfflineAsync("ep-alice", BobAddress, "hello", "9");

        Assert.Equal(SendOutcome.Failed, failed);
        Assert.Equal(SendOutcome.Stored, retried);
        Assert.Equal(["hello"], stored.WaitingFor(Bob));
    }

    [Fact]
    public async Task Stored_messages_older_than_the_retention_are_dropped()
    {
        audience.MakeMutual(Alice, Bob);
        await BobSignedOutAsync();
        await RegisterAsync("ep-alice", Alice, AliceAddress);
        await SendOfflineAsync("ep-alice", BobAddress, "old", "1");
        time.Advance(chatOptions.StoredMessageRetention - TimeSpan.FromHours(1));
        await SendOfflineAsync("ep-alice", BobAddress, "young", "2");
        time.Advance(TimeSpan.FromHours(2));

        await messages.DropExpiredStoredAsync();

        Assert.Equal(["young"], stored.WaitingFor(Bob));
    }

    [Fact]
    public async Task A_resent_transaction_is_answered_the_same_and_not_delivered_twice()
    {
        audience.MakeMutual(Alice, Bob);
        await BothOnlineAsync();

        var first = await SendAsync("ep-alice", BobAddress, "hello", transactionId: "7");
        var second = await SendAsync("ep-alice", BobAddress, "hello", transactionId: "7");

        Assert.Equal(SendOutcome.Delivered, first);
        Assert.Equal(SendOutcome.Delivered, second);
        Assert.Equal(["hello"], deliveries.MessagesFor("ep-bob"));
    }

    [Fact]
    public async Task Sending_faster_than_the_bucket_refills_is_throttled()
    {
        audience.MakeMutual(Alice, Bob);
        await BothOnlineAsync();

        for (var i = 0; i < chatOptions.MessageBurst; i++)
            Assert.Equal(SendOutcome.Delivered, await SendAsync("ep-alice", BobAddress, "spam", transactionId: $"{i}"));

        Assert.Equal(SendOutcome.Throttled, await SendAsync("ep-alice", BobAddress, "spam", transactionId: "over"));

        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(SendOutcome.Delivered, await SendAsync("ep-alice", BobAddress, "spam", transactionId: "later"));
    }

    [Fact]
    public async Task Data_is_limited_by_bytes_not_by_message_count()
    {
        audience.MakeMutual(Alice, Bob);
        await BothOnlineAsync();

        for (var i = 0; i < 5; i++)
            Assert.Equal(SendOutcome.Delivered, await SendAsync("ep-alice", BobAddress, new string('x', 20), $"{i}", MessageKind.Data, "device-ep-bob"));

        Assert.Equal(SendOutcome.Throttled, await SendAsync("ep-alice", BobAddress, "x", "over", MessageKind.Data, "device-ep-bob"));
        Assert.Equal(SendOutcome.Delivered, await SendAsync("ep-alice", BobAddress, "text still works", "text"));
    }

    [Fact]
    public async Task Data_goes_only_to_the_endpoint_it_names_and_is_never_echoed()
    {
        audience.MakeMutual(Alice, Bob);
        await BothOnlineAsync();
        await RegisterAsync("ep-bob-2", Bob, BobAddress);
        await RegisterAsync("ep-alice-2", Alice, AliceAddress);
        deliveries.Reset();

        var outcome = await SendAsync("ep-alice", BobAddress, "p2p", kind: MessageKind.Data, instanceId: "DEVICE-EP-BOB-2");

        Assert.Equal(SendOutcome.Delivered, outcome);
        Assert.Equal(["p2p"], deliveries.MessagesFor("ep-bob-2"));
        Assert.Empty(deliveries.MessagesFor("ep-bob"));
        Assert.Empty(deliveries.MessagesFor("ep-alice-2"));
    }

    [Fact]
    public async Task Messages_waiting_for_a_dropped_session_follow_the_device_to_its_new_one()
    {
        audience.MakeMutual(Alice, Bob);
        await BothOnlineAsync();
        await SendAsync("ep-alice", BobAddress, "sent while bob was off wifi");
        await SendAsync("ep-alice", BobAddress, "p2p chunk", transactionId: "13", kind: MessageKind.Data, instanceId: "device-ep-bob");
        await presence.SetPresenceAsync("ep-alice", PresenceStatus.Busy, CancellationToken.None);

        await presence.RegisterEndpointAsync(
            "ep-bob-new", Bob, BobAddress, PresenceStatus.Online, SessionTimeout, "device-ep-bob", CancellationToken.None);

        Assert.Equal(["sent while bob was off wifi", "p2p chunk"], deliveries.MessagesFor("ep-bob-new"));
        Assert.Empty(deliveries.MessagesFor("ep-bob"));
        Assert.Empty(deliveries.PresenceFor("ep-bob"));
        Assert.Equal([(AliceAddress, PresenceStatus.Busy)], deliveries.PresenceFor("ep-bob-new"));
    }

    [Fact]
    public async Task Data_for_an_endpoint_that_is_gone_is_offline()
    {
        audience.MakeMutual(Alice, Bob);
        await BothOnlineAsync();

        var named = await SendAsync("ep-alice", BobAddress, "p2p", kind: MessageKind.Data, instanceId: "device-long-gone");
        var unnamed = await SendAsync("ep-alice", BobAddress, "p2p", transactionId: "13", kind: MessageKind.Data);

        Assert.Equal(SendOutcome.RecipientOffline, named);
        Assert.Equal(SendOutcome.RecipientOffline, unnamed);
        Assert.Empty(deliveries.MessagesFor("ep-bob"));
    }

    [Fact]
    public async Task An_expired_audience_cache_is_refreshed_before_the_gate()
    {
        audience.MakeMutual(Alice, Bob);
        await BothOnlineAsync();
        state.ForgetAudience(Alice);

        var outcome = await SendAsync("ep-alice", BobAddress, "hello");

        Assert.Equal(SendOutcome.Delivered, outcome);
    }

    [Fact]
    public async Task No_audience_and_no_mailbox_fails_instead_of_voiding()
    {
        audience.MakeMutual(Alice, Bob);
        await BothOnlineAsync();
        state.ForgetAudience(Alice);
        audience.Unavailable = true;

        var outcome = await SendAsync("ep-alice", BobAddress, "hello");

        Assert.Equal(SendOutcome.Failed, outcome);
        Assert.Empty(deliveries.MessagesFor("ep-bob"));
    }

    [Fact]
    public async Task A_failed_send_can_be_retried()
    {
        audience.MakeMutual(Alice, Bob);
        await BothOnlineAsync();
        state.ForgetAudience(Alice);
        audience.Unavailable = true;
        await SendAsync("ep-alice", BobAddress, "hello", transactionId: "9");

        audience.Unavailable = false;
        var retried = await SendAsync("ep-alice", BobAddress, "hello", transactionId: "9");

        Assert.Equal(SendOutcome.Delivered, retried);
    }

    [Fact]
    public async Task An_unregistered_sender_fails()
    {
        var outcome = await SendAsync("ep-nobody", BobAddress, "hello");

        Assert.Equal(SendOutcome.Failed, outcome);
    }

    [Fact]
    public async Task A_recipient_whose_queue_is_full_is_offline_for_the_online_channel()
    {
        audience.MakeMutual(Alice, Bob);
        await BothOnlineAsync();
        deliveries.MaxQueuedPerEndpoint = 0;

        var outcome = await SendAsync("ep-alice", BobAddress, "hello");

        Assert.Equal(SendOutcome.RecipientOffline, outcome);
        Assert.Empty(deliveries.MessagesFor("ep-bob"));
    }

    [Fact]
    public async Task A_message_for_a_full_queue_waits_in_the_store_and_the_endpoint_is_evicted()
    {
        audience.MakeMutual(Alice, Bob);
        await BothOnlineAsync();
        deliveries.MaxQueuedPerEndpoint = 0;

        var outcome = await SendOfflineAsync("ep-alice", BobAddress, "while you were busy");

        Assert.Equal(SendOutcome.Stored, outcome);
        Assert.Equal(["while you were busy"], stored.WaitingFor(Bob));

        await presence.SweepAsync(CancellationToken.None);
        Assert.False(state.HasEndpoint("ep-bob"));

        deliveries.MaxQueuedPerEndpoint = int.MaxValue;
        await RegisterAsync("ep-bob-again", Bob, BobAddress);
        Assert.Equal(["while you were busy"], deliveries.MessagesFor("ep-bob-again"));
        Assert.Empty(stored.WaitingFor(Bob));
    }

    [Fact]
    public async Task A_full_queue_on_one_endpoint_still_delivers_to_the_others()
    {
        audience.MakeMutual(Alice, Bob);
        await BothOnlineAsync();
        await RegisterAsync("ep-bob-2", Bob, BobAddress);
        deliveries.Reset();
        deliveries.MaxQueuedPerEndpoint = 1;
        var fullEndpoint = await state.FindEndpointAsync("ep-bob");
        await deliveries.EnqueueAsync(fullEndpoint!, new ChatDelivery());

        var outcome = await SendAsync("ep-alice", BobAddress, "hello");

        Assert.Equal(SendOutcome.Delivered, outcome);
        Assert.Empty(deliveries.MessagesFor("ep-bob"));
        Assert.Equal(["hello"], deliveries.MessagesFor("ep-bob-2"));
        Assert.Equal(["ep-bob"], await deliveries.ClaimOverflowingAsync(time.Now));
    }
}
