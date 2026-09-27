using ReLiveWP.Dav;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

public class CalDavCalendarSyncDriver(
    ConnectedServicesProxy proxy,
    IConfiguration configuration,
    ILogger<CalDavCalendarSyncDriver> logger) : IMirrorDriver
{
    public const string ServiceName = "caldav";

    private const string Vevent = "VEVENT";

    private static readonly TimeSpan DefaultWindowBack = TimeSpan.FromDays(31);
    private static readonly TimeSpan DefaultWindowForward = TimeSpan.FromDays(365);
    private const int DefaultMaxInstances = 500;

    private static readonly string SyncTokenBody = DavBody.Propfind(DavProps.SyncToken, DavProps.GetCTag);

    private static readonly string ListBody = DavBody.Propfind(
        DavProps.ResourceType, DavProps.DisplayName, DavProps.SupportedCalendarComponentSet);

    private static string QueryBody(ExpansionWindow window) => DavBody.CalendarQuery(
        DavBody.ComponentFilter(Vevent, window.From, window.To), DavProps.GetETag, DavProps.CalendarData);

    public string ServiceId => ServiceName;

    public MirrorKind Kind => MirrorKind.Calendar;

    public async Task<IReadOnlyList<RemoteSource>> ListSourcesAsync(
        SyncConnection connection, CancellationToken ct = default)
    {
        var multistatus = await SendAsync(connection, DavMethods.Propfind, "", ListBody, depth: "1", ct);
        var sources = new List<RemoteSource>();

        foreach (var response in multistatus.Responses)
        {
            if (!response.IsResourceType(DavProps.Calendar)) continue;
            if (!SupportsEvents(response)) continue;

            var relative = DavPath.StripPrefix(response.Href, connection.ServiceUrl);
            var name = response.DisplayName;

            sources.Add(new RemoteSource(
                relative,
                string.IsNullOrWhiteSpace(name) ? relative : name));
        }

        return sources;
    }

    public async Task<MirrorBatch> FetchChangesAsync(
        SyncConnection connection, string sourceId, string? deltaToken, CancellationToken ct = default)
    {
        if (deltaToken is not null)
        {
            try
            {
                return await FetchDeltaAsync(connection, sourceId, deltaToken, ct);
            }
            catch (DeltaTokenExpiredException e)
            {
                logger.LogInformation("CalDAV sync token for {Source} is stale ({Reason}), falling back to a full pull",
                    sourceId, e.Message);
            }
        }

        var window = GetExpansionWindow();

        var multistatus = await SendAsync(
            connection, DavMethods.Report, sourceId, QueryBody(window), depth: "1", ct);

        var (events, unreadable) = ReadEvents(multistatus, connection, window);
        var token = await ReadSyncTokenAsync(connection, sourceId, ct);

        return new MirrorBatch(events, [], token, IsFullSync: true, unreadable);
    }

    private async Task<MirrorBatch> FetchDeltaAsync(
        SyncConnection connection, string sourceId, string deltaToken, CancellationToken ct)
    {
        var body = DavBody.SyncCollection(deltaToken, 1, DavProps.GetETag, DavProps.CalendarData);

        var multistatus = await SendAsync(connection, DavMethods.Report, sourceId, body, depth: "1", ct);

        var (events, unreadable) = ReadEvents(multistatus, connection, GetExpansionWindow());

        var deleted = multistatus.NotFound
            .Select(r => DavPath.StripPrefix(r.Href, connection.ServiceUrl))
            .ToList();

        return new MirrorBatch(events, deleted, multistatus.SyncToken, IsFullSync: false, unreadable);
    }

    private (List<IRemoteItem> Events, List<string> Unreadable) ReadEvents(
        DavMultiStatus multistatus, SyncConnection connection, ExpansionWindow window)
    {
        var events = new List<IRemoteItem>();
        var unreadable = new List<string>();

        foreach (var response in multistatus.Found)
        {
            var externalId = DavPath.StripPrefix(response.Href, connection.ServiceUrl);

            if (externalId.Length == 0 || externalId.EndsWith('/')) continue;

            var data = response.Value(DavProps.CalendarData);
            if (string.IsNullOrWhiteSpace(data))
            {
                logger.LogWarning("no calendar-data returned for {Href}", response.Href);
                unreadable.Add(externalId);
                continue;
            }

            try
            {
                var projected = ICalendarProjection.Project(externalId, response.ETag, data, window);

                if (projected.Events.Count == 0)
                {
                    unreadable.Add(externalId);
                    continue;
                }

                if (projected.ExpandedBecause is { } reason)
                    logger.LogInformation("expanded {Href} into {Count} instances: {Reason}",
                        response.Href, projected.Events.Count, reason);

                events.AddRange(projected.Events);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "could not parse the calendar object at {Href}", response.Href);
                unreadable.Add(externalId);
            }
        }

        return (events, unreadable);
    }

    private ExpansionWindow GetExpansionWindow()
    {
        var now = DateTime.UtcNow;

        return new ExpansionWindow(
            now - configuration.GetValue("Mirror:Calendar:ExpandWindowBack", DefaultWindowBack),
            now + configuration.GetValue("Mirror:Calendar:ExpandWindowForward", DefaultWindowForward),
            configuration.GetValue("Mirror:Calendar:ExpandMaxInstances", DefaultMaxInstances));
    }

    // RFC 4791 5.2.3. an absent property means the collection takes anything, which in practice is
    // a server that only ever holds events.
    private static bool SupportsEvents(DavResponse response)
    {
        if (response.Prop(DavProps.SupportedCalendarComponentSet) is not { } set) return true;

        return set.Elements(DavProps.Comp)
            .Any(c => string.Equals((string?)c.Attribute("name"), Vevent, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string?> ReadSyncTokenAsync(SyncConnection connection, string sourceId, CancellationToken ct)
    {
        try
        {
            var multistatus = await SendAsync(connection, DavMethods.Propfind, sourceId, SyncTokenBody, depth: "0", ct);

            return multistatus.SyncToken
                ?? multistatus.Responses.Select(r => r.Value(DavProps.SyncToken)).FirstOrDefault(v => v is not null)
                ?? multistatus.Responses.Select(r => r.Value(DavProps.GetCTag)).FirstOrDefault(v => v is not null);
        }
        catch (MirrorException e)
        {
            logger.LogInformation("CalDAV collection {Source} reports no sync token ({Reason}); every poll will be a full pull",
                sourceId, e.Message);
            return null;
        }
    }

    private async Task<DavMultiStatus> SendAsync(
        SyncConnection connection, HttpMethod method, string path, string body, string depth, CancellationToken ct)
    {
        using var dav = proxy.CreateDavClient(ServiceName, connection);

        try
        {
            return method == DavMethods.Report
                ? await dav.ReportAsync(path, body, depth, ct)
                : await dav.PropfindAsync(path, body, depth, ct);
        }
        catch (DavSyncTokenException e)
        {
            throw new DeltaTokenExpiredException(e.Message);
        }
        catch (DavParseException e)
        {
            throw new MirrorException($"CalDAV {method} {path}: {e.Message}");
        }
        catch (DavException e)
        {
            throw new MirrorException($"CalDAV {method} {path} failed: {e.Message}");
        }
    }
}
