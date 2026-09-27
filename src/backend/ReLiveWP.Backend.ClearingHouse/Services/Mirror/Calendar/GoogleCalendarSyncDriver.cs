using System.Text.Json;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

public class GoogleCalendarSyncDriver(
    ConnectedServicesProxy proxy,
    IConfiguration configuration,
    ILogger<GoogleCalendarSyncDriver> logger) : IMirrorDriver
{
    public const string ServiceName = "google";

    private const string Host = "www.googleapis.com";
    private const int PageSize = 250;
    private const int MaxPages = 200;

    private static readonly TimeSpan DefaultWindowBack = TimeSpan.FromDays(31);
    private static readonly TimeSpan DefaultWindowForward = TimeSpan.FromDays(365);
    private const int DefaultMaxInstances = 500;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public string ServiceId => ServiceName;

    public MirrorKind Kind => MirrorKind.Calendar;

    public async Task<IReadOnlyList<RemoteSource>> ListSourcesAsync(
        SyncConnection connection, CancellationToken ct = default)
    {
        var sources = new List<RemoteSource>();
        string? pageToken = null;
        var pages = 0;

        do
        {
            var path = $"{Host}/calendar/v3/users/me/calendarList?maxResults={PageSize}";
            if (pageToken is not null) path += $"&pageToken={Uri.EscapeDataString(pageToken)}";

            var response = Deserialize<GoogleCalendarListResponse>(await SendAsync(connection, path, ct));

            foreach (var entry in response?.Items ?? [])
            {
                if (entry.Id is not { Length: > 0 } id || entry.Deleted) continue;

                var name = entry.SummaryOverride is { Length: > 0 } over ? over : entry.Summary;

                sources.Add(new RemoteSource(id, string.IsNullOrWhiteSpace(name) ? id : name,
                    IsDefault: entry.Primary));
            }

            pageToken = response?.NextPageToken;
        } while (pageToken is not null && ++pages < MaxPages);

        return sources;
    }

    public async Task<MirrorBatch> FetchChangesAsync(
        SyncConnection connection, string sourceId, string? deltaToken, CancellationToken ct = default)
    {
        var batch = await PullAsync(connection, sourceId, deltaToken, ct);

        // a delta can hand back a changed instance without its master, and a series is stored as one
        // item carrying every exception, so applying that alone would drop the ones already known.
        // pulling the calendar in full is cheaper than reassembling the series request by request.
        if (batch is null)
        {
            logger.LogInformation("a recurring instance changed on {Source}; pulling the calendar in full", sourceId);
            batch = await PullAsync(connection, sourceId, null, ct);
        }

        return batch!;
    }

    private async Task<MirrorBatch?> PullAsync(
        SyncConnection connection, string sourceId, string? deltaToken, CancellationToken ct)
    {
        var full = deltaToken is null;

        var events = new List<GoogleEvent>();
        string? pageToken = null;
        string? nextSyncToken = null;
        var pages = 0;

        do
        {
            var path = $"{Host}/calendar/v3/calendars/{Uri.EscapeDataString(sourceId)}/events" +
                       $"?singleEvents=false&showDeleted=true&maxResults={PageSize}";

            if (deltaToken is not null) path += $"&syncToken={Uri.EscapeDataString(deltaToken)}";
            if (pageToken is not null) path += $"&pageToken={Uri.EscapeDataString(pageToken)}";

            var body = await SendAsync(connection, path, ct, expired: deltaToken is not null);
            var page = Deserialize<GoogleEventsResponse>(body);

            events.AddRange(page?.Items ?? []);

            pageToken = page?.NextPageToken;
            nextSyncToken = page?.NextSyncToken ?? nextSyncToken;

            if (++pages > MaxPages)
                throw new MirrorException($"Google returned more than {MaxPages} pages of events; refusing to truncate.");
        } while (pageToken is not null);

        // an override arriving without its master is what forces the full pull above
        if (!full && events.Any(e => e.RecurringEventId is { Length: > 0 }))
            return null;

        return Assemble(events, nextSyncToken, full);
    }

    private MirrorBatch Assemble(List<GoogleEvent> events, string? syncToken, bool full)
    {
        var overrides = events
            .Where(e => e.RecurringEventId is { Length: > 0 })
            .GroupBy(e => e.RecurringEventId!)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<GoogleEvent>)[.. g], StringComparer.Ordinal);

        var items = new List<IRemoteItem>();
        var deleted = new List<string>();
        var unreadable = new List<string>();
        var window = Window();

        foreach (var source in events.Where(e => e.RecurringEventId is not { Length: > 0 }))
        {
            if (source.Id is not { Length: > 0 } id) continue;

            // google auto-creates these as all-day "Office"/"Home" markers. they are not
            // appointments and would bury a real calendar on the phone.
            if (string.Equals(source.EventType, "workingLocation", StringComparison.OrdinalIgnoreCase))
                continue;

            // a cancelled master is a real delete; a cancelled instance is folded in as an exception
            if (GoogleCalendarProjection.IsCancelled(source))
            {
                deleted.Add(id);
                continue;
            }

            try
            {
                var projected = GoogleCalendarProjection.Project(
                    source, overrides.GetValueOrDefault(id, []), window);

                if (projected.ExpandedBecause is { } reason)
                    logger.LogInformation("expanded google event {Event} into {Count} instances: {Reason}",
                        id, projected.Events.Count, reason);

                items.AddRange(projected.Events);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogWarning(e, "could not project the google event {Event}", id);
                unreadable.Add(id);
            }
        }

        return new MirrorBatch(items, deleted, syncToken, full, unreadable);
    }

    private ExpansionWindow Window()
    {
        var now = DateTime.UtcNow;

        return new ExpansionWindow(
            now - configuration.GetValue("Mirror:Calendar:ExpandWindowBack", DefaultWindowBack),
            now + configuration.GetValue("Mirror:Calendar:ExpandWindowForward", DefaultWindowForward),
            configuration.GetValue("Mirror:Calendar:ExpandMaxInstances", DefaultMaxInstances));
    }

    private async Task<string> SendAsync(
        SyncConnection connection, string path, CancellationToken ct, bool expired = false)
    {
        using var request = proxy.Request(HttpMethod.Get, ServiceName, path, connection);
        using var response = await proxy.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);

        var body = await response.Content.ReadAsStringAsync(ct);

        if ((int)response.StatusCode == 410 && expired)
            throw new DeltaTokenExpiredException("Google rejected the sync token; a full sync is required.");

        if (!response.IsSuccessStatusCode)
            throw new MirrorException(
                $"Google Calendar {path} failed ({(int)response.StatusCode}): {ConnectedServicesProxy.Truncate(body)}");

        return body;
    }

    private static T? Deserialize<T>(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(body, Json);
        }
        catch (JsonException e)
        {
            throw new MirrorException($"Google Calendar returned unreadable JSON: {e.Message}");
        }
    }
}
