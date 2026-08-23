using System.Text.Json;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

public class GraphCalendarSyncDriver(
    ConnectedServicesProxy proxy,
    IConfiguration configuration,
    ILogger<GraphCalendarSyncDriver> logger) : IMirrorDriver
{
    public const string ServiceName = "microsoft";

    private const string Host = "graph.microsoft.com";
    private const int PageSize = 100;
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
        var path = $"{Host}/v1.0/me/calendars?$select=id,name,isDefaultCalendar&$top={PageSize}";

        for (var page = 0; page < MaxPages && path is not null; page++)
        {
            var response = Deserialize<GraphCalendarsResponse>(await SendAsync(connection, path, ct));

            foreach (var calendar in response?.Value ?? [])
            {
                if (calendar.Id is not { Length: > 0 } id) continue;

                sources.Add(new RemoteSource(id, calendar.Name is { Length: > 0 } name ? name : id,
                    IsDefault: calendar.IsDefaultCalendar));
            }

            path = response?.NextLink is { Length: > 0 } next ? GetProxiedUrl(next) : null;
        }

        return sources;
    }

    public async Task<MirrorBatch> FetchChangesAsync(
        SyncConnection connection, string sourceId, string? deltaToken, CancellationToken ct = default)
    {
        var events = new List<GraphEvent>();
        var path = $"{Host}/v1.0/me/calendars/{Uri.EscapeDataString(sourceId)}/events?$top={PageSize}";

        for (var page = 0; page < MaxPages && path is not null; page++)
        {
            var response = Deserialize<GraphEventsResponse>(await SendAsync(connection, path, ct));

            events.AddRange(response?.Value ?? []);
            path = response?.NextLink is { Length: > 0 } next ? GetProxiedUrl(next) : null;

            if (path is not null && page == MaxPages - 1)
                throw new MirrorException($"Graph returned more than {MaxPages} pages of events; refusing to truncate.");
        }

        var items = new List<IRemoteItem>();
        var unreadable = new List<string>();
        var window = GetExpansionWindow();

        foreach (var source in events)
        {
            if (source.Id is not { Length: > 0 } id) continue;
            if (source.IsCancelled) continue;

            try
            {
                var master = source.Recurrence is not null
                    ? await WithOccurrencesAsync(connection, source, ct)
                    : source;

                var projected = GraphCalendarProjection.Project(master, window);

                if (projected.ExpandedBecause is { } reason)
                    logger.LogInformation("expanded graph event {Event} into {Count} instances: {Reason}",
                        id, projected.Events.Count, reason);

                items.AddRange(projected.Events);
            }
            catch (MirrorException)
            {
                throw;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogWarning(e, "could not project the graph event {Event}", id);
                unreadable.Add(id);
            }
        }

        return new MirrorBatch(items, [], null, IsFullSync: true, unreadable);
    }

    private const string OccurrenceSelect =
        "id,cancelledOccurrences,exceptionOccurrences,iCalUId,subject,bodyPreview,start,end," +
        "originalStart,originalStartTimeZone,isAllDay,isCancelled,showAs,sensitivity,location," +
        "organizer,attendees,categories,onlineMeetingUrl,lastModifiedDateTime," +
        "isReminderOn,reminderMinutesBeforeStart";

    private async Task<GraphEvent> WithOccurrencesAsync(
        SyncConnection connection, GraphEvent master, CancellationToken ct)
    {
        var path = $"{Host}/v1.0/me/events/{Uri.EscapeDataString(master.Id!)}" +
                   $"?$select={OccurrenceSelect}&$expand=exceptionOccurrences";

        var detail = Deserialize<GraphEvent>(await SendAsync(connection, path, ct));
        if (detail is null) return master;

        master.CancelledOccurrences = detail.CancelledOccurrences;
        master.ExceptionOccurrences = detail.ExceptionOccurrences;

        return master;
    }

    private ExpansionWindow GetExpansionWindow()
    {
        var now = DateTime.UtcNow;

        return new ExpansionWindow(
            now - configuration.GetValue("Mirror:Calendar:ExpandWindowBack", DefaultWindowBack),
            now + configuration.GetValue("Mirror:Calendar:ExpandWindowForward", DefaultWindowForward),
            configuration.GetValue("Mirror:Calendar:ExpandMaxInstances", DefaultMaxInstances));
    }

    private static string GetProxiedUrl(string absolute) =>
        absolute.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? absolute[8..] : absolute;

    private async Task<string> SendAsync(SyncConnection connection, string path, CancellationToken ct)
    {
        using var request = proxy.Request(HttpMethod.Get, ServiceName, path, connection);
        request.Headers.TryAddWithoutValidation("Prefer", $"odata.maxpagesize={PageSize}");

        using var response = await proxy.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new MirrorException(
                $"Graph {path} failed ({(int)response.StatusCode}): {ConnectedServicesProxy.Truncate(body)}");

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
            throw new MirrorException($"Graph returned unreadable JSON: {e.Message}");
        }
    }
}
