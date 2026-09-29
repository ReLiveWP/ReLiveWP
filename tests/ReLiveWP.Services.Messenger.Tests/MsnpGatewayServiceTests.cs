using System.Text;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Chat;
using ReLiveWP.Services.Messenger.Msnp;
using ReLiveWP.Services.Messenger.Services;
using static ReLiveWP.Services.Messenger.Tests.MsnpSamples;

namespace ReLiveWP.Services.Messenger.Tests;

public class MsnpGatewayServiceTests
{
    private const string OpenBatch =
        "VER 1 MSNP21\r\n" +
        "CVR 2 0x0409 winnt 7.5 arm WLM 7.5.7720 MSNMSGR alice@example.com\r\n" +
        "USR 3 SSO I alice@example.com\r\n";

    private static readonly string SignInBatch =
        $"USR 4 SSO S t=ticket p= secret {Epid}\r\n" +
        "FSL 5 579 0\r\n" +
        "CHL 6\r\n" +
        SelfPresencePut("7");

    private readonly InMemorySessionStore sessions = new();
    private readonly FakeChatClient chat = new();
    private readonly RecordingSessionWaker waker = new();
    private readonly InMemoryDeviceStore devices = new();

    private static VerifyResponse Verified()
    {
        var response = new VerifyResponse { Code = 0, Id = "user-alice" };
        response.Claims.Add(new ClaimMessage { Type = "email", Value = "alice@example.com" });
        return response;
    }

    private MsnpGatewayService CreateGateway(Func<VerifyTokenRequest, VerifyResponse> verifyToken) =>
        CreateGateway(new FakeAuthenticationClient(verifyToken));

    private MsnpGatewayService CreateGateway(FakeAuthenticationClient auth, MessengerOptions? options = null) =>
        new(sessions, devices, auth, chat, waker, Options.Create(options ?? new MessengerOptions()), NullLogger<MsnpGatewayService>.Instance);

    private async Task<(MsnpGatewayService Gateway, string SessionId)> SignedInAsync(MessengerOptions? options = null)
    {
        var gateway = CreateGateway(new FakeAuthenticationClient(_ => Verified()), options);
        var sessionId = await OpenAsync(gateway);
        await PollLinesAsync(gateway, sessionId, SignInBatch);
        return (gateway, sessionId);
    }

    private static async Task<string> OpenAsync(MsnpGatewayService gateway)
    {
        var (sessionId, _) = await gateway.OpenAsync(
            Parse(OpenBatch), "https://push.relivewp.net/channel/token", 259200, CancellationToken.None);
        return sessionId;
    }

    private static async Task<string[]> PollLinesAsync(MsnpGatewayService gateway, string sessionId, string body)
    {
        var reply = await gateway.PollAsync(sessionId, Parse(body), TimeSpan.Zero, CancellationToken.None);
        Assert.NotNull(reply);
        return SerializeToText(reply).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
    }

    [Fact]
    public async Task SignInBatchGetsAReplyForEveryCommand()
    {
        var auth = new FakeAuthenticationClient(_ => Verified());
        var gateway = CreateGateway(auth);
        var sessionId = await OpenAsync(gateway);

        var lines = await PollLinesAsync(gateway, sessionId, SignInBatch);

        Assert.Equal(4, lines.Length);
        Assert.Equal("USR 4 OK alice@example.com 1 0", lines[0]);
        Assert.Equal("FSL 5 OK 0", lines[1]);
        Assert.Matches(@"^CHL 0 \d{20}$", lines[2]);
        Assert.Equal("PUT 7 OK 0", lines[3]);

        var request = Assert.Single(auth.Requests);
        Assert.Equal("ticket", request.Token);
    }

    [Fact]
    public async Task QryIsAcked()
    {
        var gateway = CreateGateway(_ => Verified());
        var sessionId = await OpenAsync(gateway);
        await PollLinesAsync(gateway, sessionId, SignInBatch);

        var lines = await PollLinesAsync(gateway, sessionId, $"QRY 8 APPID 32\r\n{new string('a', 32)}");

        Assert.Equal(["QRY 8"], lines);
    }

    [Fact]
    public async Task RejectedTicketGets911AndNothingElse()
    {
        var gateway = CreateGateway(_ => new VerifyResponse { Code = 0x80048821 });
        var sessionId = await OpenAsync(gateway);

        var lines = await PollLinesAsync(gateway, sessionId, SignInBatch);

        Assert.Equal(["911 4"], lines);
        Assert.Empty(sessions.Sessions);
    }

    [Fact]
    public async Task MissingTicketGets911()
    {
        var gateway = CreateGateway(_ => Verified());
        var sessionId = await OpenAsync(gateway);

        var lines = await PollLinesAsync(gateway, sessionId, $"USR 4 SSO S p= secret {Epid}\r\nFSL 5 579 0\r\n");

        Assert.Equal(["911 4"], lines);
        Assert.Empty(sessions.Sessions);
    }

    [Fact]
    public async Task UnreachableIdentityGets500()
    {
        var gateway = CreateGateway(_ => throw new RpcException(new Status(StatusCode.Unavailable, "down")));
        var sessionId = await OpenAsync(gateway);

        var lines = await PollLinesAsync(gateway, sessionId, SignInBatch);

        Assert.Equal(["500 4"], lines);
        Assert.Empty(sessions.Sessions);
    }

    [Fact]
    public async Task CommandsBeforeSignInAreIgnored()
    {
        var gateway = CreateGateway(_ => Verified());
        var sessionId = await OpenAsync(gateway);

        var lines = await PollLinesAsync(gateway, sessionId, "FSL 5 579 0\r\nCHL 6\r\n" + SelfPresencePut("7"));

        Assert.Empty(lines);
        Assert.Null(sessions.Sessions[sessionId].Status);
    }

    [Fact]
    public async Task OpenWithoutUsrDoesNotCreateASession()
    {
        var gateway = CreateGateway(_ => Verified());

        var (sessionId, reply) = await gateway.OpenAsync(Parse("OUT\r\n"), null, null, CancellationToken.None);

        Assert.NotEmpty(sessionId);
        Assert.Empty(reply.Commands);
        Assert.Empty(sessions.Sessions);
    }

    [Fact]
    public async Task OutEndsTheSession()
    {
        var gateway = CreateGateway(_ => Verified());
        var sessionId = await OpenAsync(gateway);
        await PollLinesAsync(gateway, sessionId, SignInBatch);

        var reply = await gateway.PollAsync(sessionId, Parse("OUT\r\n"), TimeSpan.Zero, CancellationToken.None);

        Assert.Null(reply);
        Assert.Empty(sessions.Sessions);
        Assert.Equal(sessionId, Assert.Single(chat.Ended).EndpointId);
    }

    [Fact]
    public async Task SignInRegistersTheEndpointWithChat()
    {
        var gateway = CreateGateway(_ => Verified());
        var sessionId = await OpenAsync(gateway);

        await PollLinesAsync(gateway, sessionId, SignInBatch);

        var registered = Assert.Single(chat.Registered);
        Assert.Equal(sessionId, registered.EndpointId);
        Assert.Equal("user-alice", registered.UserId);
        Assert.Equal("alice@example.com", registered.Address);
        Assert.Equal(PresenceStatus.Online, registered.Status);
        Assert.Equal(259200, registered.SessionTimeoutSeconds);
        Assert.Equal(Guid.Parse(Epid).ToString("D"), registered.InstanceId);
        Assert.True(sessions.Sessions[sessionId].ChatRegistered);
    }

    [Fact]
    public async Task AnEpidThatIsNotAGuidIsNeverRegistered()
    {
        var gateway = CreateGateway(_ => Verified());
        var sessionId = await OpenAsync(gateway);
        var injectedPresence = PresencePayload.Replace("epid=" + Epid, "epid={\" x=\"}");
        var batch =
            $"USR 4 SSO S t=ticket p= secret {Epid}\r\n" +
            $"PUT 5 {Encoding.UTF8.GetByteCount(injectedPresence)}\r\n{injectedPresence}";

        await PollLinesAsync(gateway, sessionId, batch);

        Assert.Null(sessions.Sessions[sessionId].EndpointId);
        Assert.Equal("", Assert.Single(chat.Registered).InstanceId);
    }

    [Fact]
    public async Task LaterPollsReportActivity()
    {
        var gateway = CreateGateway(_ => Verified());
        var sessionId = await OpenAsync(gateway);
        await PollLinesAsync(gateway, sessionId, SignInBatch);

        await PollLinesAsync(gateway, sessionId, "");

        Assert.Equal(sessionId, Assert.Single(chat.ActivityReported).EndpointId);
        Assert.Single(chat.Registered);
    }

    [Fact]
    public async Task AStatusChangeGoesToChat()
    {
        var gateway = CreateGateway(_ => Verified());
        var sessionId = await OpenAsync(gateway);
        await PollLinesAsync(gateway, sessionId, SignInBatch);

        var busy = PresencePayload.Replace("NLN", "BSY");
        await PollLinesAsync(gateway, sessionId, $"PUT 8 {busy.Length}\r\n{busy}");

        var set = Assert.Single(chat.PresenceSet);
        Assert.Equal(PresenceStatus.Busy, set.Status);
        Assert.Empty(chat.ActivityReported);
    }

    [Fact]
    public async Task AnEndpointChatForgotIsRegisteredAgain()
    {
        var gateway = CreateGateway(_ => Verified());
        var sessionId = await OpenAsync(gateway);
        await PollLinesAsync(gateway, sessionId, SignInBatch);
        chat.EndpointState = EndpointState.Unknown;

        await PollLinesAsync(gateway, sessionId, "");

        Assert.Equal(2, chat.Registered.Count);
    }

    [Fact]
    public async Task AReplacedSessionStopsTalkingToChat()
    {
        var gateway = CreateGateway(_ => Verified());
        var sessionId = await OpenAsync(gateway);
        await PollLinesAsync(gateway, sessionId, SignInBatch);
        chat.EndpointState = EndpointState.Superseded;

        await PollLinesAsync(gateway, sessionId, "");
        await PollLinesAsync(gateway, sessionId, "");

        Assert.Single(chat.Registered);
        Assert.Single(chat.ActivityReported);
        Assert.True(sessions.Sessions[sessionId].ChatSuperseded);
    }

    [Fact]
    public async Task AnEvictedEndpointClosesTheSession()
    {
        var (gateway, sessionId) = await SignedInAsync();
        chat.EndpointState = EndpointState.Evicted;

        var reply = await gateway.PollAsync(sessionId, Parse(""), TimeSpan.Zero, CancellationToken.None);

        Assert.Null(reply);
        Assert.Empty(sessions.Sessions);
        Assert.Single(chat.Registered);
        Assert.Empty(devices.DevicesOf("user-alice"));
    }

    [Fact]
    public async Task ChatBeingDownDoesNotBreakSignIn()
    {
        chat.FailWith = StatusCode.Unavailable;
        var gateway = CreateGateway(_ => Verified());
        var sessionId = await OpenAsync(gateway);

        var lines = await PollLinesAsync(gateway, sessionId, SignInBatch);

        Assert.Equal("USR 4 OK alice@example.com 1 0", lines[0]);
        Assert.False(sessions.Sessions[sessionId].ChatRegistered);
    }

    [Fact]
    public async Task ChatDeliveriesBecomePresenceNotifications()
    {
        var gateway = CreateGateway(_ => Verified());
        var sessionId = await OpenAsync(gateway);
        await PollLinesAsync(gateway, sessionId, SignInBatch);

        sessions.QueueChatDelivery(sessionId, Presence("bob@example.com", PresenceStatus.Busy));
        sessions.QueueChatDelivery(sessionId, Presence("carol@example.com", PresenceStatus.Offline));
        sessions.QueueChatDelivery(sessionId, Presence("alice@example.com", PresenceStatus.Offline));

        var reply = await gateway.PollAsync(sessionId, Parse(""), TimeSpan.Zero, CancellationToken.None);

        Assert.NotNull(reply);
        Assert.Equal(2, reply.Commands.Count);

        var busy = reply.Commands[0];
        Assert.Equal(("NFY", "PUT"), (busy.Verb, busy.TrId));
        Assert.Contains("From: 1:bob@example.com\r\n", busy.PayloadText);
        Assert.Contains("To: 1:alice@example.com\r\n", busy.PayloadText);
        Assert.EndsWith("<user><s n=\"IM\"><Status>BSY</Status></s></user>", busy.PayloadText);

        var gone = reply.Commands[1];
        Assert.Equal(("NFY", "DEL"), (gone.Verb, gone.TrId));
        Assert.Contains("From: 1:carol@example.com\r\n", gone.PayloadText);
        Assert.EndsWith("<user><s n=\"IM\" /></user>", gone.PayloadText);
    }

    [Fact]
    public async Task PushedPresenceWakesTheSession()
    {
        var gateway = CreateGateway(_ => Verified());
        var sessionId = await OpenAsync(gateway);

        await gateway.PushPresenceAsync(
            sessionId, MsnpMuri.ForWindowsLive("bob@example.com"), PresenceStatus.Online, MsnpNotifType.Full, CancellationToken.None);

        Assert.Equal([sessionId], waker.Woken);
    }

    [Fact]
    public async Task APollLeavesTheLeftoverCheckToTheWaker()
    {
        var (gateway, sessionId) = await SignedInAsync();
        waker.Woken.Clear();

        await gateway.PollAsync(sessionId, Parse(""), TimeSpan.Zero, CancellationToken.None);

        Assert.Equal([sessionId], waker.Woken);
    }

    [Fact]
    public async Task SdgIsHandedToChatWithItsBytesUntouched()
    {
        var (gateway, sessionId) = await SignedInAsync();
        var payload = SdgPayload("h" + (char)0xE9 + "llo");

        var lines = await PollLinesAsync(gateway, sessionId, Sdg("12", payload));

        Assert.Empty(lines);
        var sent = Assert.Single(chat.Sent);
        Assert.Equal(sessionId, sent.EndpointId);
        Assert.Equal("12", sent.TransactionId);
        Assert.Equal("bob@example.com", sent.RecipientAddress);
        Assert.Equal(MessageKind.Text, sent.Kind);
        Assert.Equal(Encoding.UTF8.GetBytes(payload), sent.Payload.ToByteArray());
    }

    [Fact]
    public async Task SdgInTheSignInBatchIsSentAfterTheEndpointIsRegistered()
    {
        var gateway = CreateGateway(_ => Verified());
        var sessionId = await OpenAsync(gateway);

        await PollLinesAsync(gateway, sessionId, SignInBatch + Sdg("8", SdgPayload("hi")));

        Assert.Equal(["RegisterEndpoint", "SendMessage"], chat.Calls);
    }

    [Theory]
    [InlineData("1:mallory@example.com;epid=" + Epid)]
    [InlineData("1:alice@example.com;epid={99999999-2222-3333-4444-555555555555}")]
    [InlineData("1:alice@example.com")]
    [InlineData("1:alice@example.com;epid={\" x=\"}")]
    public async Task SdgFromSomeoneElseIsDroppedWithoutAReply(string from)
    {
        var (gateway, sessionId) = await SignedInAsync();

        var lines = await PollLinesAsync(gateway, sessionId, Sdg("12", SdgPayload("hi", from: from)));

        Assert.Empty(lines);
        Assert.Empty(chat.Sent);
    }

    [Fact]
    public async Task SdgClaimingToBeFromSomeoneElseCannotHideBehindALaterBlock()
    {
        var (gateway, sessionId) = await SignedInAsync();
        var forged = SdgPayload("hi", from: "1:mallory@example.com")
            .Replace("Reliability: 1.0\r\n", "Reliability: 1.0\r\nFrom: 1:alice@example.com;epid=" + Epid + "\r\n");

        var lines = await PollLinesAsync(gateway, sessionId, Sdg("12", forged));

        Assert.Empty(lines);
        Assert.Empty(chat.Sent);
    }

    [Theory]
    [InlineData("10:00000000-0000-0000-0000-000000000000@live.com", "Text")]
    [InlineData("1:bob@example.com", "Signal/CloseIMWindow")]
    [InlineData("1:bob@example.com", "Control/Typing")]
    public async Task SdgWeDoNotRelayIsDroppedWithoutAReply(string to, string messageType)
    {
        var (gateway, sessionId) = await SignedInAsync();

        var lines = await PollLinesAsync(gateway, sessionId, Sdg("12", SdgPayload("hi", to: to, messageType: messageType)));

        Assert.Empty(lines);
        Assert.Empty(chat.Sent);
    }

    [Fact]
    public async Task NudgeAndDataAreRelayed()
    {
        var (gateway, sessionId) = await SignedInAsync();

        await PollLinesAsync(gateway, sessionId,
            Sdg("12", SdgPayload("", messageType: "Nudge")) + Sdg("13", SdgPayload("bytes", messageType: "Data")));

        Assert.Equal([MessageKind.Nudge, MessageKind.Data], chat.Sent.Select(s => s.Kind));
    }

    [Fact]
    public async Task OversizedSdgGets800AndNeverReachesChat()
    {
        var (gateway, sessionId) = await SignedInAsync(new MessengerOptions { MaxTextSdgBytes = 64 });

        var lines = await PollLinesAsync(gateway, sessionId, Sdg("12", SdgPayload(new string('x', 64))));

        Assert.Equal(["800 12"], lines);
        Assert.Empty(chat.Sent);
    }

    [Theory]
    [InlineData("IM/Online", "Text", "217 12")]
    [InlineData("IM/Offline", "Text", "201 12")]
    [InlineData("IM/Online", "Data", "201 12")]
    public async Task OfflineRecipientGetsTheErrorThePhoneExpects(string channel, string messageType, string expected)
    {
        var (gateway, sessionId) = await SignedInAsync();
        chat.SendOutcome = SendOutcome.RecipientOffline;

        var lines = await PollLinesAsync(gateway, sessionId, Sdg("12", SdgPayload("hi", channel: channel, messageType: messageType)));

        Assert.Equal([expected], lines);
    }

    [Theory]
    [InlineData(SendOutcome.Delivered, new string[0])]
    [InlineData(SendOutcome.Voided, new string[0])]
    [InlineData(SendOutcome.Stored, new string[0])]
    [InlineData(SendOutcome.Throttled, new[] { "800 12" })]
    [InlineData(SendOutcome.Failed, new[] { "500 12" })]
    public async Task ChatOutcomesMapToReplies(SendOutcome outcome, string[] expected)
    {
        var (gateway, sessionId) = await SignedInAsync();
        chat.SendOutcome = outcome;

        var lines = await PollLinesAsync(gateway, sessionId, Sdg("12", SdgPayload("hi")));

        Assert.Equal(expected, lines);
    }

    [Fact]
    public async Task ChatBeingDownFailsTheSdgWith500()
    {
        var (gateway, sessionId) = await SignedInAsync();
        chat.FailWith = StatusCode.Unavailable;

        var lines = await PollLinesAsync(gateway, sessionId, Sdg("12", SdgPayload("hi")));

        Assert.Equal(["500 12"], lines);
    }

    [Fact]
    public async Task MessageDeliveriesBecomeSdgsWithThePayloadAsIs()
    {
        var (gateway, sessionId) = await SignedInAsync();
        var incoming = Encoding.UTF8.GetBytes(SdgPayload("hi alice", to: "1:alice@example.com", from: "1:bob@example.com"));
        var echo = Encoding.UTF8.GetBytes(SdgPayload("sent from my other phone"));

        sessions.QueueChatDelivery(sessionId, Message(incoming));
        sessions.QueueChatDelivery(sessionId, Message(echo));

        var reply = await gateway.PollAsync(sessionId, Parse(""), TimeSpan.Zero, CancellationToken.None);

        Assert.NotNull(reply);
        Assert.Equal(2, reply.Commands.Count);
        Assert.All(reply.Commands, c => Assert.Equal(("SDG", "0"), (c.Verb, c.TrId)));
        Assert.Equal(incoming, reply.Commands[0].Payload);
        Assert.Equal(echo, reply.Commands[1].Payload);
        Assert.StartsWith($"SDG 0 {incoming.Length}\r\nRouting: 1.0\r\n", SerializeToText(reply));
    }

    [Theory]
    [InlineData("IM/Offline", "Text", true)]
    [InlineData("IM/Offline", "Nudge", true)]
    [InlineData("IM/Online", "Text", false)]
    [InlineData("IM/Offline", "Data", false)]
    public async Task OnlyOfflineChannelTextAndNudgesMayBeStored(string channel, string messageType, bool storable)
    {
        var (gateway, sessionId) = await SignedInAsync();
        var payload = SdgPayload("hi", channel: channel, messageType: messageType);

        await PollLinesAsync(gateway, sessionId, Sdg("12", payload));

        var sent = Assert.Single(chat.Sent);
        Assert.Equal(storable, sent.StoreIfOffline);
        Assert.Equal(payload, sent.Payload.ToStringUtf8());
    }

    [Fact]
    public async Task LiveOfflineChannelMessagesAreWrittenAsOnline()
    {
        var (gateway, sessionId) = await SignedInAsync();
        sessions.QueueChatDelivery(sessionId, Message(Encoding.UTF8.GetBytes(SdgPayload("hi", channel: "IM/Offline"))));

        var reply = await gateway.PollAsync(sessionId, Parse(""), TimeSpan.Zero, CancellationToken.None);

        Assert.NotNull(reply);
        Assert.Equal(SdgPayload("hi", channel: "IM/Online"), Encoding.UTF8.GetString(Assert.Single(reply.Commands).Payload!));
    }

    [Fact]
    public async Task StoredMessagesAreWrittenOfflineWithWhenTheyWereSent()
    {
        var (gateway, sessionId) = await SignedInAsync();
        var sentAt = new DateTimeOffset(2026, 9, 27, 21, 4, 5, 67, TimeSpan.Zero);
        var stored = new ChatDelivery
        {
            MessageReceived = new MessageReceived
            {
                Payload = ByteString.CopyFromUtf8(SdgPayload("while you were out", channel: "IM/Offline")),
                OriginalArrivalUnixMs = sentAt.ToUnixTimeMilliseconds(),
            },
        };
        sessions.QueueChatDelivery(sessionId, stored);

        var reply = await gateway.PollAsync(sessionId, Parse(""), TimeSpan.Zero, CancellationToken.None);

        Assert.NotNull(reply);
        var written = Encoding.UTF8.GetString(Assert.Single(reply.Commands).Payload!);
        Assert.Contains("\r\nService-Channel: IM/Offline\r\n", written);
        Assert.Contains("\r\nContent-Transfer-Encoding: 7bit\r\nOriginal-Arrival-Time: 2026-09-27T21:04:05.067Z\r\n\r\nwhile you were out", written);
    }

    [Fact]
    public async Task SignInRemembersTheDeviceForTheDoorbell()
    {
        await SignedInAsync();

        var device = Assert.Single(devices.DevicesOf("user-alice"));
        Assert.Equal(Guid.Parse(Epid).ToString("D"), device.InstanceId);
        Assert.Equal("https://push.relivewp.net/channel/token", device.NotificationUri);
    }

    [Fact]
    public async Task SigningOutForgetsTheDevice()
    {
        var (gateway, sessionId) = await SignedInAsync();

        await gateway.PollAsync(sessionId, Parse("OUT\r\n"), TimeSpan.Zero, CancellationToken.None);

        Assert.Empty(devices.DevicesOf("user-alice"));
    }

    [Fact]
    public async Task DataIsRelayedToTheEndpointItNamesWithoutRewriting()
    {
        var (gateway, sessionId) = await SignedInAsync();
        var payload = SdgPayload("p2p", to: "1:bob@example.com;epid={AAAAAAAA-2222-3333-4444-555555555555}", channel: "PE", messageType: "Data");

        await PollLinesAsync(gateway, sessionId, Sdg("12", payload));

        var sent = Assert.Single(chat.Sent);
        Assert.Equal("aaaaaaaa-2222-3333-4444-555555555555", sent.RecipientInstanceId);
        Assert.Equal(payload, sent.Payload.ToStringUtf8());
    }

    [Fact]
    public async Task PresenceListsTheContactsEndpoints()
    {
        var (gateway, sessionId) = await SignedInAsync();
        var change = new PresenceChanged { Address = "bob@example.com", Status = PresenceStatus.Online };
        change.InstanceIds.Add("AAAAAAAA-2222-3333-4444-555555555555");
        change.InstanceIds.Add("BBBBBBBB-2222-3333-4444-555555555555");
        sessions.QueueChatDelivery(sessionId, new ChatDelivery { PresenceChanged = change });

        var reply = await gateway.PollAsync(sessionId, Parse(""), TimeSpan.Zero, CancellationToken.None);

        var nfy = Assert.Single(reply!.Commands);
        Assert.EndsWith(
            "<user><s n=\"IM\"><Status>NLN</Status></s>" +
            "<sep n=\"IM\" epid=\"{AAAAAAAA-2222-3333-4444-555555555555}\" />" +
            "<sep n=\"IM\" epid=\"{BBBBBBBB-2222-3333-4444-555555555555}\" /></user>",
            nfy.PayloadText);
    }

    [Fact]
    public async Task InstanceIdsThatAreNotGuidsNeverReachThePresenceDocument()
    {
        var (gateway, sessionId) = await SignedInAsync();
        var change = new PresenceChanged { Address = "bob@example.com", Status = PresenceStatus.Online };
        change.InstanceIds.Add("aaaaaaaa-2222-3333-4444-555555555555");
        change.InstanceIds.Add("\"/><s n=\"PE\"/><sep x=\"");
        change.RemovedInstanceIds.Add("<evil/>");
        sessions.QueueChatDelivery(sessionId, new ChatDelivery { PresenceChanged = change });

        var reply = await gateway.PollAsync(sessionId, Parse(""), TimeSpan.Zero, CancellationToken.None);

        var nfy = Assert.Single(reply!.Commands);
        Assert.EndsWith(
            "<user><s n=\"IM\"><Status>NLN</Status></s>" +
            "<sep n=\"IM\" epid=\"{AAAAAAAA-2222-3333-4444-555555555555}\" /></user>",
            nfy.PayloadText);
    }

    [Fact]
    public async Task AContactsDeviceLeavingIsRemovedBeforeTheUpdate()
    {
        var (gateway, sessionId) = await SignedInAsync();
        var change = new PresenceChanged { Address = "bob@example.com", Status = PresenceStatus.Online };
        change.InstanceIds.Add("AAAAAAAA-2222-3333-4444-555555555555");
        change.RemovedInstanceIds.Add("BBBBBBBB-2222-3333-4444-555555555555");
        sessions.QueueChatDelivery(sessionId, new ChatDelivery { PresenceChanged = change });

        var reply = await gateway.PollAsync(sessionId, Parse(""), TimeSpan.Zero, CancellationToken.None);

        Assert.Equal(2, reply!.Commands.Count);
        Assert.Equal(("NFY", "DEL"), (reply.Commands[0].Verb, reply.Commands[0].TrId));
        Assert.EndsWith("<user><sep n=\"IM\" epid=\"{BBBBBBBB-2222-3333-4444-555555555555}\" /></user>", reply.Commands[0].PayloadText);
        Assert.Equal(("NFY", "PUT"), (reply.Commands[1].Verb, reply.Commands[1].TrId));
    }

    [Fact]
    public async Task APollCancelledAfterDrainingPutsItsWorkBack()
    {
        var (gateway, sessionId) = await SignedInAsync();
        sessions.QueueChatDelivery(sessionId, Message(Encoding.UTF8.GetBytes(SdgPayload("hi alice"))));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => gateway.PollAsync(sessionId, Parse(""), TimeSpan.Zero, cancelled.Token));

        var retried = await gateway.PollAsync(sessionId, Parse(""), TimeSpan.Zero, CancellationToken.None);
        var sdg = Assert.Single(retried!.Commands);
        Assert.Equal("SDG", sdg.Verb);
    }

    private static ChatDelivery Presence(string address, PresenceStatus status) =>
        new() { PresenceChanged = new PresenceChanged { Address = address, Status = status } };

    private static ChatDelivery Message(byte[] payload) =>
        new() { MessageReceived = new MessageReceived { Payload = ByteString.CopyFrom(payload) } };
}
