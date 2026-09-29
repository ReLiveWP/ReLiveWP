using Grpc.Core;
using Microsoft.Extensions.Options;
using RedLockNet;
using ReLiveWP.Backend.Chat.Data;
using ReLiveWP.Backend.Chat.Utilities;
using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Backend.Chat.Services;

public class PresenceService(
    IChatStateStore state,
    IAudienceSource audienceSource,
    IDeliveryQueue deliveries,
    IStoredMessageStore storedMessages,
    IDistributedLockFactory locks,
    IOptions<ChatOptions> options,
    TimeProvider time,
    ILogger<PresenceService> logger)
{
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LockWait = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LockRetry = TimeSpan.FromMilliseconds(50);

    private readonly record struct AudienceChange(
        PresenceAudience Audience,
        IReadOnlyCollection<string> CanSeeAdded,
        IReadOnlyCollection<string> CanSeeRemoved,
        IReadOnlyCollection<string> SeenByAdded,
        IReadOnlyCollection<string> SeenByRemoved);

    private TimeSpan ActiveWindow => options.Value.ActiveWindow;

    public async Task RegisterEndpointAsync(
        string endpointId, string userId, string address, PresenceStatus status, TimeSpan sessionTimeout,
        string instanceId, CancellationToken ct)
    {
        await using var userLock = await LockUserAsync(userId, ct);

        var now = time.GetUtcNow();
        var existing = await state.ListEndpointsAsync(userId);
        var otherEndpoints = new List<ChatEndpoint>();
        var replaced = new List<ChatEndpoint>();

        foreach (var other in existing.Where(e => e.EndpointId != endpointId))
        {
            if (!IsSameDevice(other, instanceId))
            {
                otherEndpoints.Add(other);
                continue;
            }

            await state.RemoveEndpointAsync(other);
            await state.MarkGoneAsync(other, EndpointState.Superseded);
            replaced.Add(other);
        }

        var evicted = await EvictOldestEndpointsAsync(userId, otherEndpoints);

        var newDevice = instanceId.Length > 0 && !existing.Any(e => IsSameDevice(e, instanceId));
        var endpoint = new ChatEndpoint(endpointId, userId, address, status, Active: true, sessionTimeout, now, instanceId);
        await state.SaveEndpointAsync(endpoint);
        await state.TouchEndpointAsync(endpoint, now, ActiveWindow);

        var removedInstanceIds = evicted
            .Select(e => e.InstanceId)
            .Where(id => id.Length > 0 && !IsSameDevice(endpoint, id) && !otherEndpoints.Any(e => IsSameDevice(e, id)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (replaced.Count > 0)
            removedInstanceIds.Add(instanceId);

        foreach (var old in replaced)
        {
            var moved = await deliveries.MoveMessagesAsync(old.EndpointId, endpoint);
            ChatMetrics.RecordEndpointEvent("replaced");
            logger.LogInformation("endpoint {Endpoint} replaces {Replaced} for {User}, same device {Instance}, moved {Moved} undelivered messages",
                endpointId, old.EndpointId, userId, instanceId, moved);
        }

        var shownBefore = (await state.FindUserAsync(userId))?.Shown ?? PresenceStatus.Offline;
        var change = await TryRefreshAudienceAsync(userId, ct);
        if (change is { } refreshed)
            await ApplyAudienceChangeAsync(userId, address, shownBefore, refreshed, otherEndpoints);

        var audience = change?.Audience ?? await GetAudienceAsync(userId, ct);
        await SendSnapshotAsync(endpoint, audience.CanSee);
        await DeliverStoredMessagesAsync(userId);
        await RecomputeShownAsync(userId, audience, ct, ChoiceFrom(status),
            devicesChanged: newDevice || removedInstanceIds.Count > 0,
            removedInstanceIds: removedInstanceIds);

        ChatMetrics.RecordEndpointEvent("registered");
        logger.LogInformation("registered endpoint {Endpoint} for {User} as {Status}, can see {CanSee}, seen by {SeenBy}",
            endpointId, userId, status, audience.CanSee.Count, audience.SeenBy.Count);
    }

    private async Task<IReadOnlyList<ChatEndpoint>> EvictOldestEndpointsAsync(string userId, List<ChatEndpoint> otherEndpoints)
    {
        var excess = otherEndpoints.Count + 1 - options.Value.MaxEndpointsPerUser;
        if (excess <= 0)
            return [];

        var evicted = otherEndpoints.OrderBy(e => e.LastActivity).Take(excess).ToList();
        foreach (var old in evicted)
        {
            await state.RemoveEndpointAsync(old);
            await state.MarkGoneAsync(old, EndpointState.Evicted);
            await deliveries.ClearAsync(old.EndpointId);
            otherEndpoints.Remove(old);

            ChatMetrics.RecordEndpointEvent("evicted");
            logger.LogInformation("evicted endpoint {Endpoint} for {User}, last active {LastActivity}, to stay within {Max} endpoints",
                old.EndpointId, userId, old.LastActivity, options.Value.MaxEndpointsPerUser);
        }

        return evicted;
    }

    public async Task DeliverStoredMessagesAsync(string userId)
    {
        var endpoints = await state.ListEndpointsAsync(userId);
        var delivered = await storedMessages.DeliverStoredAsync(userId, endpoints);
        if (delivered == 0)
            return;

        ChatMetrics.RecordStoredMessages("delivered", delivered);
        logger.LogInformation("delivered {Count} stored messages to {User} on {Endpoints} endpoints", delivered, userId, endpoints.Count);
    }

    public async Task<EndpointState> SetPresenceAsync(string endpointId, PresenceStatus status, CancellationToken ct)
    {
        if (await state.FindEndpointAsync(endpointId) is not { } found)
            return await MissingEndpointAsync(endpointId);

        await using var userLock = await LockUserAsync(found.UserId, ct);
        if (await state.FindEndpointAsync(endpointId) is not { } endpoint)
            return await MissingEndpointAsync(endpointId);

        var now = time.GetUtcNow();
        var updated = endpoint with { Status = status, Active = true, LastActivity = now };
        await state.SaveEndpointAsync(updated);
        await state.TouchEndpointAsync(updated, now, ActiveWindow);
        await RecomputeShownAsync(updated.UserId, null, ct, ChoiceFrom(status), throttled: true);
        return EndpointState.Known;
    }

    private static PresenceStatus? ChoiceFrom(PresenceStatus published) =>
        PresenceRules.IsUserChoice(published) ? published : null;

    public async Task<EndpointState> ReportActivityAsync(string endpointId, CancellationToken ct)
    {
        if (await state.FindEndpointAsync(endpointId) is not { } found)
            return await MissingEndpointAsync(endpointId);

        var now = time.GetUtcNow();
        await state.TouchEndpointAsync(found, now, ActiveWindow);
        if (found.Active)
            return EndpointState.Known;

        await using var userLock = await LockUserAsync(found.UserId, ct);
        if (await state.FindEndpointAsync(endpointId) is not { } endpoint)
            return await MissingEndpointAsync(endpointId);

        if (endpoint.Active)
            return EndpointState.Known;

        await state.SaveEndpointAsync(endpoint with { Active = true, LastActivity = now });
        await RecomputeShownAsync(endpoint.UserId, null, ct);

        ChatMetrics.RecordEndpointEvent("active");
        logger.LogDebug("endpoint {Endpoint} for {User} is active again", endpointId, endpoint.UserId);
        return EndpointState.Known;
    }

    private Task<EndpointState> MissingEndpointAsync(string endpointId) => state.FindGoneReasonAsync(endpointId);

    private static bool IsSameDevice(ChatEndpoint endpoint, string instanceId) =>
        instanceId.Length > 0 && string.Equals(endpoint.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase);

    public async Task EndEndpointAsync(string endpointId, CancellationToken ct)
    {
        if (await state.FindEndpointAsync(endpointId) is not { } found)
            return;

        await using var userLock = await LockUserAsync(found.UserId, ct);
        if (await state.FindEndpointAsync(endpointId) is not { } endpoint)
            return;

        await RemoveEndpointAsync(endpoint, ct);
        ChatMetrics.RecordEndpointEvent("ended");
        logger.LogInformation("ended endpoint {Endpoint} for {User}", endpointId, endpoint.UserId);
    }

    public async Task SweepAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow();

        var quiet = await state.ClaimQuietDueAsync(now);
        await SweepClaimedAsync("quiet", quiet,
            endpointId => MarkQuietAsync(endpointId, now, ct),
            endpointId => state.ScheduleQuietCheckAsync(endpointId, now), ct);

        var expired = await state.ClaimExpiredAsync(now);
        await SweepClaimedAsync("expiry", expired,
            endpointId => ExpireAsync(endpointId, now, ct),
            endpointId => state.ScheduleExpiryCheckAsync(endpointId, now), ct);

        var overflowing = await deliveries.ClaimOverflowingAsync(now);
        await SweepClaimedAsync("overflow", overflowing,
            endpointId => EvictOverflowingAsync(endpointId, ct),
            endpointId => deliveries.ScheduleOverflowEvictionAsync(endpointId, now), ct);

        var announcements = await state.ClaimPresenceAnnouncementsAsync(now);
        await SweepClaimedAsync("presence announcement", announcements,
            userId => AnnouncePresenceAsync(userId, ct),
            userId => state.SchedulePresenceAnnouncementAsync(userId, now), ct);
    }

    // a claim takes the ids out of the schedule, so anything not handled has to go back in or it's never looked at again
    private async Task SweepClaimedAsync(
        string sweep, IReadOnlyList<string> claimed, Func<string, Task> handle, Func<string, Task> scheduleAgain, CancellationToken ct)
    {
        for (var i = 0; i < claimed.Count; i++)
        {
            try
            {
                await handle(claimed[i]);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                await ScheduleAgainAsync(sweep, claimed.Skip(i), scheduleAgain);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "{Sweep} sweep failed for {Id}, trying it again next sweep", sweep, claimed[i]);
                await ScheduleAgainAsync(sweep, [claimed[i]], scheduleAgain);
            }
        }
    }

    private async Task ScheduleAgainAsync(string sweep, IEnumerable<string> ids, Func<string, Task> scheduleAgain)
    {
        foreach (var id in ids)
        {
            try
            {
                await scheduleAgain(id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "could not put {Id} back on the {Sweep} schedule, it won't be swept again", id, sweep);
            }
        }
    }

    private async Task EvictOverflowingAsync(string endpointId, CancellationToken ct)
    {
        if (await state.FindEndpointAsync(endpointId) is not { } found)
            return;

        await using var userLock = await LockUserAsync(found.UserId, ct);
        if (await state.FindEndpointAsync(endpointId) is not { } endpoint)
            return;

        await state.MarkGoneAsync(endpoint, EndpointState.Evicted);
        await RemoveEndpointAsync(endpoint, ct);

        ChatMetrics.RecordEndpointEvent("overflowed");
        logger.LogWarning("evicted endpoint {Endpoint} for {User}, its delivery queue filled up without being drained",
            endpointId, endpoint.UserId);
    }

    private async Task AnnouncePresenceAsync(string userId, CancellationToken ct)
    {
        await using var userLock = await LockUserAsync(userId, ct);
        await RecomputeShownAsync(userId, null, ct, throttled: true);
    }

    public async Task RefreshAudienceAsync(string userId, CancellationToken ct)
    {
        var endpoints = await state.ListEndpointsAsync(userId);
        if (endpoints.Count == 0 && await state.GetAudienceAsync(userId) is null)
            return;

        await using var userLock = await LockUserAsync(userId, ct);

        endpoints = await state.ListEndpointsAsync(userId);
        var user = await state.FindUserAsync(userId);
        if (await TryRefreshAudienceAsync(userId, ct) is not { } change)
            return;

        var address = endpoints.FirstOrDefault()?.Address ?? user?.Address;
        await ApplyAudienceChangeAsync(userId, address, user?.Shown ?? PresenceStatus.Offline, change, endpoints);
    }

    private async Task MarkQuietAsync(string endpointId, DateTimeOffset now, CancellationToken ct)
    {
        if (await state.FindEndpointAsync(endpointId) is not { } found)
            return;

        await using var userLock = await LockUserAsync(found.UserId, ct);
        if (await state.FindEndpointAsync(endpointId) is not { } endpoint)
            return;

        if (!endpoint.Active || endpoint.LastActivity + ActiveWindow > now)
            return;

        await state.SaveEndpointAsync(endpoint with { Active = false });
        await RecomputeShownAsync(endpoint.UserId, null, ct);

        ChatMetrics.RecordEndpointEvent("quiet");
        logger.LogDebug("endpoint {Endpoint} for {User} went quiet, last active {LastActivity}",
            endpointId, endpoint.UserId, endpoint.LastActivity);
    }

    private async Task ExpireAsync(string endpointId, DateTimeOffset now, CancellationToken ct)
    {
        if (await state.FindEndpointAsync(endpointId) is not { } found)
            return;

        await using var userLock = await LockUserAsync(found.UserId, ct);
        if (await state.FindEndpointAsync(endpointId) is not { } endpoint)
            return;

        if (endpoint.LastActivity + endpoint.SessionTimeout > now)
            return;

        await RemoveEndpointAsync(endpoint, ct);
        ChatMetrics.RecordEndpointEvent("expired");
        logger.LogInformation("expired endpoint {Endpoint} for {User}, quiet since {LastActivity}",
            endpointId, endpoint.UserId, endpoint.LastActivity);
    }

    private async Task RemoveEndpointAsync(ChatEndpoint endpoint, CancellationToken ct)
    {
        await state.RemoveEndpointAsync(endpoint);
        await deliveries.ClearAsync(endpoint.EndpointId);

        var remaining = await state.ListEndpointsAsync(endpoint.UserId);
        var deviceGone = endpoint.InstanceId.Length > 0 && !remaining.Any(e => IsSameDevice(e, endpoint.InstanceId));
        await RecomputeShownAsync(endpoint.UserId, null, ct,
            devicesChanged: deviceGone, removedInstanceIds: deviceGone ? [endpoint.InstanceId] : []);
    }

    private async Task RecomputeShownAsync(
        string userId, PresenceAudience? audience, CancellationToken ct, PresenceStatus? newChoice = null,
        bool devicesChanged = false, IReadOnlyList<string>? removedInstanceIds = null, bool throttled = false)
    {
        var endpoints = await state.ListEndpointsAsync(userId);
        var user = await state.FindUserAsync(userId);
        var previous = user?.Shown ?? PresenceStatus.Offline;

        string address;
        PresenceStatus shown;
        if (endpoints.Count == 0)
        {
            if (user is null)
                return;

            await state.ForgetUserAsync(user);
            address = user.Address;
            shown = PresenceStatus.Offline;
        }
        else
        {
            var chosen = newChoice ?? user?.Chosen ?? PresenceStatus.Online;
            shown = PresenceRules.Show(chosen, endpoints);
            address = endpoints[0].Address;
            var lifetime = endpoints.Max(e => e.SessionTimeout);

            if (throttled && shown != previous && !await TryTakePresenceAllowanceAsync(userId))
            {
                await state.SaveUserAsync(new ChatUser(userId, address, previous, chosen), lifetime);
                await state.SchedulePresenceAnnouncementAsync(userId, time.GetUtcNow() + options.Value.PresenceRetryAfter);

                ChatMetrics.RecordPresenceDeferred();
                logger.LogDebug("{User} is changing status too often, telling watchers {Status} later instead of now", userId, shown);
                return;
            }

            var current = new ChatUser(userId, address, shown, chosen);
            if (current != user)
                await state.SaveUserAsync(current, lifetime);
        }

        var devicesMoved = devicesChanged && shown != PresenceStatus.Offline;
        if (shown == previous && !devicesMoved)
            return;

        audience ??= await GetAudienceAsync(userId, ct);
        var delivery = await PresenceDeliveryAsync(userId, address, shown, removedInstanceIds ?? []);
        await FanOutAsync(delivery, audience.SeenBy);

        ChatMetrics.RecordPresenceChange(shown, devicesOnly: shown == previous);
        logger.LogInformation("{User} shows as {Status} (was {Previous}) on {Devices} devices, told {Count} watchers",
            userId, shown, previous, delivery.PresenceChanged.InstanceIds.Count, audience.SeenBy.Count);
    }

    private Task<bool> TryTakePresenceAllowanceAsync(string userId)
    {
        var limits = options.Value;
        return state.TryTakeTokensAsync(
            $"presence:{userId}", 1, limits.PresenceBurst, limits.PresenceRefillPerSecond, time.GetUtcNow());
    }

    private async Task FanOutAsync(ChatDelivery delivery, IEnumerable<string> watcherIds)
    {
        foreach (var watcherId in watcherIds)
            await DeliverToUserAsync(watcherId, delivery);
    }

    private async Task SendSnapshotAsync(ChatEndpoint endpoint, IReadOnlySet<string> canSee)
    {
        foreach (var peerId in canSee)
        {
            if (await state.FindUserAsync(peerId) is not { Shown: not PresenceStatus.Offline } peer)
                continue;

            var delivery = await PresenceDeliveryAsync(peer.UserId, peer.Address, peer.Shown, []);
            await deliveries.EnqueueAsync(endpoint, delivery);
        }
    }

    private async Task ApplyAudienceChangeAsync(
        string userId, string? address, PresenceStatus shown, AudienceChange change, IReadOnlyList<ChatEndpoint> ownEndpoints)
    {
        foreach (var peerId in change.CanSeeRemoved)
            await ShowPeerAsync(peerId, ownEndpoints, visible: false);

        foreach (var peerId in change.CanSeeAdded)
            await ShowPeerAsync(peerId, ownEndpoints, visible: true);

        if (address is null || shown == PresenceStatus.Offline)
            return;

        var gone = await PresenceDeliveryAsync(userId, address, PresenceStatus.Offline, []);
        await FanOutAsync(gone, change.SeenByRemoved);

        var shownNow = await PresenceDeliveryAsync(userId, address, shown, []);
        await FanOutAsync(shownNow, change.SeenByAdded);
    }

    private async Task ShowPeerAsync(string peerId, IReadOnlyList<ChatEndpoint> ownEndpoints, bool visible)
    {
        if (await state.FindUserAsync(peerId) is not { Shown: not PresenceStatus.Offline } peer)
            return;

        var status = visible ? peer.Shown : PresenceStatus.Offline;
        var delivery = await PresenceDeliveryAsync(peer.UserId, peer.Address, status, []);
        foreach (var endpoint in ownEndpoints)
            await deliveries.EnqueueAsync(endpoint, delivery);
    }

    private async Task DeliverToUserAsync(string userId, ChatDelivery delivery)
    {
        foreach (var endpoint in await state.ListEndpointsAsync(userId))
            await deliveries.EnqueueAsync(endpoint, delivery);
    }

    private async Task<PresenceAudience> GetAudienceAsync(string userId, CancellationToken ct)
    {
        if (await state.GetAudienceAsync(userId) is { } cached)
            return cached;

        return (await TryRefreshAudienceAsync(userId, ct))?.Audience ?? PresenceAudience.Empty;
    }

    private async Task<AudienceChange?> TryRefreshAudienceAsync(string userId, CancellationToken ct)
    {
        PresenceAudience fetched;
        try
        {
            fetched = await audienceSource.GetAudienceAsync(userId, ct);
        }
        catch (RpcException ex)
        {
            ChatMetrics.RecordAudienceRefresh("unavailable");
            logger.LogWarning(ex, "could not work out who {User} can see, keeping the audience it had", userId);
            return null;
        }

        var audience = new PresenceAudience(
            fetched.CanSee.Where(id => id != userId).ToHashSet(StringComparer.Ordinal),
            fetched.SeenBy.Where(id => id != userId).ToHashSet(StringComparer.Ordinal));

        var previous = await state.GetAudienceAsync(userId) ?? PresenceAudience.Empty;
        await state.SetAudienceAsync(userId, audience, options.Value.AudienceCacheTtl);

        var change = new AudienceChange(
            audience,
            CanSeeAdded: audience.CanSee.Except(previous.CanSee).ToList(),
            CanSeeRemoved: previous.CanSee.Except(audience.CanSee).ToList(),
            SeenByAdded: audience.SeenBy.Except(previous.SeenBy).ToList(),
            SeenByRemoved: previous.SeenBy.Except(audience.SeenBy).ToList());

        Task[] cacheUpdates =
        [
            .. change.CanSeeAdded.Select(peerId => state.AddAudienceMemberIfCachedAsync(peerId, AudienceSide.SeenBy, userId)),
            .. change.CanSeeRemoved.Select(peerId => state.RemoveAudienceMemberIfCachedAsync(peerId, AudienceSide.SeenBy, userId)),
            .. change.SeenByAdded.Select(watcherId => state.AddAudienceMemberIfCachedAsync(watcherId, AudienceSide.CanSee, userId)),
            .. change.SeenByRemoved.Select(watcherId => state.RemoveAudienceMemberIfCachedAsync(watcherId, AudienceSide.CanSee, userId)),
        ];
        await Task.WhenAll(cacheUpdates);

        var changed = change.CanSeeAdded.Count + change.CanSeeRemoved.Count + change.SeenByAdded.Count + change.SeenByRemoved.Count > 0;
        ChatMetrics.RecordAudienceRefresh(changed ? "changed" : "unchanged");
        if (changed)
        {
            logger.LogInformation(
                "audience for {User} changed: can see +{CanSeeAdded} -{CanSeeRemoved}, seen by +{SeenByAdded} -{SeenByRemoved}",
                userId, change.CanSeeAdded.Count, change.CanSeeRemoved.Count, change.SeenByAdded.Count, change.SeenByRemoved.Count);
        }

        return change;
    }

    private async Task<IRedLock> LockUserAsync(string userId, CancellationToken ct)
    {
        var handle = await locks.CreateLockAsync($"chat:lock:user:{userId}", LockExpiry, LockWait, LockRetry, ct);
        if (!handle.IsAcquired)
            logger.LogWarning("could not lock {User} for a presence update, going ahead without it", userId);

        return handle;
    }

    private async Task<ChatDelivery> PresenceDeliveryAsync(
        string userId, string address, PresenceStatus status, IReadOnlyList<string> removedInstanceIds)
    {
        var change = new PresenceChanged { Address = address, Status = status };
        if (status == PresenceStatus.Offline)
            return new ChatDelivery { PresenceChanged = change };

        var endpoints = await state.ListEndpointsAsync(userId);
        change.InstanceIds.AddRange(endpoints
            .Select(e => e.InstanceId)
            .Where(id => id.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase));
        change.RemovedInstanceIds.AddRange(removedInstanceIds);

        return new ChatDelivery { PresenceChanged = change };
    }
}
