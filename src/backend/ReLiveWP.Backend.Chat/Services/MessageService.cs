using System.Diagnostics;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Options;
using ReLiveWP.Backend.Chat.Data;
using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Backend.Chat.Services;

public class MessageService(
    IChatStateStore state,
    IDeliveryQueue deliveries,
    IStoredMessageStore storedMessages,
    IUserDirectory directory,
    PresenceService presence,
    IOptions<ChatOptions> options,
    TimeProvider time,
    ILogger<MessageService> logger)
{
    public async Task<SendOutcome> SendAsync(
        string endpointId, string transactionId, string recipientAddress, string recipientInstanceId, MessageKind kind,
        ByteString payload, bool storeIfOffline, CancellationToken ct)
    {
        if (await state.FindEndpointAsync(endpointId) is not { } sender)
        {
            logger.LogWarning("message from endpoint {Endpoint}, which isn't registered", endpointId);
            return SendOutcome.Failed;
        }

        if (await state.FindSendOutcomeAsync(endpointId, transactionId) is { } repeated)
        {
            logger.LogInformation("endpoint {Endpoint} sent {TransactionId} again, answering {Outcome} like last time",
                endpointId, transactionId, repeated);
            return repeated;
        }

        var outcome = await RouteAsync(sender, recipientAddress, recipientInstanceId, kind, payload, storeIfOffline, ct);
        if (outcome != SendOutcome.Failed)
            await state.RecordSendOutcomeAsync(endpointId, transactionId, outcome, options.Value.SendDedupeWindow);

        ChatMetrics.RecordMessage(kind, outcome, payload.Length);
        Activity.Current?.SetTag("chat.message.kind", kind.ToString());
        Activity.Current?.SetTag("chat.message.outcome", outcome.ToString());
        Activity.Current?.SetTag("chat.message.bytes", payload.Length);

        logger.LogDebug("{Kind} from {User} to {Recipient} epid {Epid} ({Bytes} bytes): {Outcome}",
            kind, sender.UserId, recipientAddress, recipientInstanceId, payload.Length, outcome);
        return outcome;
    }

    private async Task<SendOutcome> RouteAsync(
        ChatEndpoint sender, string recipientAddress, string recipientInstanceId, MessageKind kind, ByteString payload,
        bool storeIfOffline, CancellationToken ct)
    {
        if (!await TryTakeAllowanceAsync(sender.UserId, kind, payload.Length))
            return SendOutcome.Throttled;

        if (await GetAudienceAsync(sender.UserId, ct) is not { } audience)
            return SendOutcome.Failed;

        string? recipientId;
        try
        {
            recipientId = await FindRecipientAsync(recipientAddress, ct);
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "could not look up a recipient for {User}, failing the send", sender.UserId);
            return SendOutcome.Failed;
        }

        var allowed = recipientId is not null && (audience.CanSee.Contains(recipientId) || audience.SeenBy.Contains(recipientId));
        if (!allowed)
        {
            await EchoToOtherEndpointsAsync(sender, kind, payload);
            return SendOutcome.Voided;
        }

        var targets = await ListTargetsAsync(recipientId!, recipientInstanceId, kind);
        var delivery = new ChatDelivery { MessageReceived = new MessageReceived { Payload = payload } };
        if (!await EnqueueToAnyAsync(targets, delivery))
        {
            if (!storeIfOffline || kind == MessageKind.Data)
                return SendOutcome.RecipientOffline;

            return await StoreForLaterAsync(sender, recipientId!, kind, payload);
        }

        await EchoToOtherEndpointsAsync(sender, kind, payload);
        return SendOutcome.Delivered;
    }

    private async Task<bool> EnqueueToAnyAsync(IReadOnlyList<ChatEndpoint> targets, ChatDelivery delivery)
    {
        var accepted = 0;
        foreach (var endpoint in targets)
        {
            if (await deliveries.EnqueueAsync(endpoint, delivery))
                accepted++;
            else
                logger.LogWarning("endpoint {Endpoint} of {User} has a full queue, not delivering to it", endpoint.EndpointId, endpoint.UserId);
        }

        return accepted > 0;
    }

    private async Task<SendOutcome> StoreForLaterAsync(ChatEndpoint sender, string recipientId, MessageKind kind, ByteString payload)
    {
        var stored = new ChatDelivery
        {
            MessageReceived = new MessageReceived
            {
                Payload = payload,
                OriginalArrivalUnixMs = time.GetUtcNow().ToUnixTimeMilliseconds(),
            },
            TraceParent = Activity.Current?.Id ?? "",
        };

        if (!await storedMessages.TryStoreAsync(recipientId, sender.UserId, stored))
        {
            ChatMetrics.RecordStoredMessages("refused", 1);
            logger.LogInformation("{User} has too many messages waiting from {Sender}, or in total, refusing another", recipientId, sender.UserId);
            return SendOutcome.Throttled;
        }

        ChatMetrics.RecordStoredMessages("stored", 1);

        // catches a sign-in that landed between looking for endpoints and storing
        await presence.DeliverStoredMessagesAsync(recipientId);

        await EchoToOtherEndpointsAsync(sender, kind, payload);
        return SendOutcome.Stored;
    }

    public async Task DropExpiredStoredAsync()
    {
        var dropped = await storedMessages.DropExpiredAsync(time.GetUtcNow());
        if (dropped == 0)
            return;

        ChatMetrics.RecordStoredMessages("expired", dropped);
        logger.LogInformation("dropped {Count} stored messages nobody signed in for", dropped);
    }

    private async Task<string?> FindRecipientAsync(string address, CancellationToken ct)
    {
        if (await state.FindUserIdByAddressAsync(address) is { } signedIn)
            return signedIn;

        return await directory.FindUserIdByAddressAsync(address, ct);
    }

    private async Task<IReadOnlyList<ChatEndpoint>> ListTargetsAsync(string recipientId, string recipientInstanceId, MessageKind kind)
    {
        var endpoints = await state.ListEndpointsAsync(recipientId);
        if (kind != MessageKind.Data)
            return endpoints;

        return endpoints
            .Where(e => recipientInstanceId.Length > 0
                && string.Equals(e.InstanceId, recipientInstanceId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async Task EchoToOtherEndpointsAsync(ChatEndpoint sender, MessageKind kind, ByteString payload)
    {
        if (kind == MessageKind.Data)
            return;

        var echo = new ChatDelivery { MessageReceived = new MessageReceived { Payload = payload } };
        foreach (var endpoint in await state.ListEndpointsAsync(sender.UserId))
        {
            if (endpoint.EndpointId != sender.EndpointId)
                await deliveries.EnqueueAsync(endpoint, echo);
        }
    }

    private Task<bool> TryTakeAllowanceAsync(string userId, MessageKind kind, int bytes)
    {
        var limits = options.Value;
        var now = time.GetUtcNow();

        if (kind == MessageKind.Data)
        {
            return state.TryTakeTokensAsync(
                $"bytes:{userId}", bytes, limits.DataBurstBytes, limits.DataRefillBytesPerSecond, now);
        }

        return state.TryTakeTokensAsync(
            $"messages:{userId}", 1, limits.MessageBurst, limits.MessageRefillPerSecond, now);
    }

    private async Task<PresenceAudience?> GetAudienceAsync(string userId, CancellationToken ct)
    {
        if (await state.GetAudienceAsync(userId) is { } cached)
            return cached;

        await presence.RefreshAudienceAsync(userId, ct);
        var refreshed = await state.GetAudienceAsync(userId);
        if (refreshed is null)
            logger.LogWarning("could not work out who {User} can message, failing the send", userId);

        return refreshed;
    }
}
