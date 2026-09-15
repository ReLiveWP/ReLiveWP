using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

public class UpdateService(UpdatesDbContext db)
{
    private const int BatchSize = 90;

    // MS-WUSP 2.2.2.2.4: "the update MUST NOT be deployed, and is used to override another
    // deployment... the server MAY refrain from sending the revision to the client at all". We take
    // that second option, because the first one does not actually stop a WP7 device: applicability
    // is computed client-side from metadata we do not change, so a revision sent with a softer
    // action still evaluates as installable, still has its bundle children pulled, and still gets
    // its file locations asked for.
    public const string BlockAction = "Block";

    private static List<CandidateRevision> ResolveNeeded(
        List<CandidateRevision> deployed, ILookup<long, Guid> bundledBy)
    {
        var reachable = UpdateScopeFilter.ResolveBundleReachable(deployed, bundledBy);

        return [.. deployed
            .Where(c => c.DeploymentAction != UpdateScopeFilter.BundleAction || reachable.Contains(c.RevisionId))
            .OrderBy(c => c.IsLeaf)
            .ThenBy(c => c.UpdateType)];
    }

    public async Task<SyncUpdatesResult> SyncUpdatesAsync(string requestXml)
    {
        var started = Stopwatch.GetTimestamp();
        var request = SyncUpdatesRequest.Parse(requestXml);
        var installed = request.InstalledNonLeafUpdateIds.ToList();
        var cached = request.Known;

        var candidates = await QueryCandidatesAsync(installed);
        var bundledBy = await QueryBundleEdgesAsync();
        var ring = await ResolveDeviceRingAsync(request);

        var latest = candidates
            .GroupBy(c => c.UpdateId)
            .Select(g => g.OrderByDescending(c => c.RevisionNumber).First())
            .ToList();

        var overrides = await QueryDeploymentOverridesAsync(ring, latest);
        var deployed = latest
            .Select(c => overrides.TryGetValue(c.UpdateId, out var applied) && applied.Action != BlockAction
                ? c with { DeploymentAction = applied.Action, LastChangeTime = applied.LastChangeTime }
                : c)
            .ToList();

        var blocked = deployed
            .Where(c => overrides.TryGetValue(c.UpdateId, out var applied) && applied.Action == BlockAction)
            .Select(c => c.RevisionId)
            .ToHashSet();

        // Resolved again as if the block did not exist, so the trace can say what it took out once
        // the bundle children fall with their parent.
        var unblocked = ResolveNeeded(deployed, bundledBy);
        var needed = blocked.Count == 0
            ? unblocked
            : ResolveNeeded([.. deployed.Where(c => !blocked.Contains(c.RevisionId))], bundledBy);

        // MS-WUSP 3.1.5.7: every cached revision that is not needed. Withholding is not enough on
        // its own, the client goes on serving what it already holds out of its own cache. Three
        // things land here: a revision a block took out, one we superseded with a higher
        // RevisionNumber under the same UpdateId, and anything cached from a catalog we do not hold.
        var served = needed.Select(c => c.RevisionId).ToHashSet();
        var outOfScope = cached.Where(id => !served.Contains(id)).ToList();

        var newUpdates = needed.Where(c => !cached.Contains(c.RevisionId)).ToList();
        var batch = newUpdates.Take(BatchSize).ToList();

        var changed = needed.Where(c => cached.Contains(c.RevisionId)).ToList();

        var truncated = newUpdates.Count > batch.Count;
        var metadata = await QueryMetadataAsync(batch);
        var xml = SyncUpdatesResponseBuilder.Build(batch, changed, metadata, outOfScope, truncated);

        var result = new SyncUpdatesResult(
            xml,
            ring,
            installed.Count,
            cached.Count,
            batch.Count,
            changed.Count,
            batch.Count(c => c.DeploymentAction == "Install"),
            batch.Count(c => c.DeploymentAction == UpdateScopeFilter.BundleAction),
            unblocked.Count - needed.Count,
            outOfScope.Count,
            truncated);

        DeviceUpdateMetrics.RecordSyncUpdates(started, result);
        return result;
    }

    private async Task<Dictionary<Guid, AppliedDeployment>> QueryDeploymentOverridesAsync(
        DeviceRing ring, List<CandidateRevision> candidates)
    {
        var ids = candidates.Select(c => c.UpdateId).Distinct().ToList();
        var policy = ring.PolicyRing();

        return await db.Deployments
            .AsNoTracking()
            .Where(d => d.Ring == policy && ids.Contains(d.UpdateId))
            .ToDictionaryAsync(d => d.UpdateId, d => new AppliedDeployment(d.Action, d.LastChangeTime));
    }

    private record AppliedDeployment(string Action, DateTime LastChangeTime);

    private async Task<DeviceRing> ResolveDeviceRingAsync(SyncUpdatesRequest request)
    {
        var markerRevisions = await db.Updates
            .AsNoTracking()
            .Where(u => u.UpdateId == DeviceRingResolver.MarkerUpdateId)
            .Select(u => u.RevisionId)
            .ToListAsync();

        return DeviceRingResolver.Resolve(
            markerRevisions,
            request.InstalledNonLeafUpdateIds,
            request.OtherCachedUpdateIds);
    }

    private async Task<List<CandidateRevision>> QueryCandidatesAsync(List<long> installed)
    {
        var started = Stopwatch.GetTimestamp();

        var satisfied = db.Updates
            .Where(u => installed.Contains(u.RevisionId))
            .Select(u => u.UpdateId);

        var candidates = await db.Updates
            .AsNoTracking()
            .Where(u => !u.Prerequisites.Any(p =>
                p.GroupId == 0 && !satisfied.Contains(p.PrerequisiteUpdateId)))
            .Where(u => !u.Prerequisites.Any(p =>
                p.GroupId > 0 && !u.Prerequisites.Any(q =>
                    q.GroupId == p.GroupId && satisfied.Contains(q.PrerequisiteUpdateId))))
            .Where(u => !u.BundledUpdates.Any(b =>
                !db.Updates.Any(child => child.UpdateId == b.BundledUpdateId)))
            .OrderBy(u => u.RevisionId)
            .Select(u => new CandidateRevision(
                u.RevisionId,
                u.UpdateId,
                u.RevisionNumber,
                u.UpdateType,
                u.IsLeaf,
                u.DeploymentAction,
                u.LastChangeTime))
            .ToListAsync();

        DeviceUpdateMetrics.RecordCandidateQuery(started, candidates.Count);
        return candidates;
    }

    private async Task<ILookup<long, Guid>> QueryBundleEdgesAsync()
    {
        var edges = await db.Bundles
            .AsNoTracking()
            .Select(b => new { b.BundleRevisionId, b.BundledUpdateId })
            .ToListAsync();

        return edges.ToLookup(e => e.BundleRevisionId, e => e.BundledUpdateId);
    }

    private async Task<Dictionary<long, string>> QueryMetadataAsync(List<CandidateRevision> batch)
    {
        if (batch.Count == 0)
            return [];

        var ids = batch.Select(c => c.RevisionId).ToList();
        return await db.Metadata
            .AsNoTracking()
            .Where(m => ids.Contains(m.RevisionId))
            .ToDictionaryAsync(m => m.RevisionId, m => m.SyncXml);
    }

    public async Task<GetExtendedUpdateInfoResult> GetExtendedUpdateInfoAsync(string requestXml)
    {
        var started = Stopwatch.GetTimestamp();
        var revisionIds = ParseRevisionIds(requestXml);
        var infoTypes = ParseStrings(requestXml, "infoTypes");
        var locales = ParseStrings(requestXml, "locales");
        var requested = revisionIds.ToList();

        var fragments = await db.Fragments
            .AsNoTracking()
            .Where(f => requested.Contains(f.UpdateRevisionId))
            .OrderBy(f => f.UpdateRevisionId)
            .ThenBy(f => f.Ordinal)
            .Select(f => new { f.UpdateRevisionId, f.FragmentType, f.Language, f.Xml })
            .ToListAsync();

        var files = await db.Files
            .AsNoTracking()
            .Where(f => requested.Contains(f.UpdateRevisionId) && f.DigestSha1 != null && f.LocalPath != null)
            .OrderBy(f => f.UpdateRevisionId)
            .ThenBy(f => f.FileName)
            .Select(f => new { f.DigestSha1, f.SourceUrl })
            .ToListAsync();

        var served = fragments.Select(f => f.UpdateRevisionId).Distinct().ToHashSet();
        var outOfScope = requested.Where(id => !served.Contains(id)).ToList();

        // stays in memory on purpose: we store pt-br, the client asks for pt-BR, and neither
        // provider's default collation matches those case-insensitively
        var wanted = fragments
            .Where(f => FragmentWanted(f.FragmentType, f.Language, infoTypes, locales))
            .Select(f => new XElement(WsupXml.Service + "Update",
                new XElement(WsupXml.Service + "ID", f.UpdateRevisionId),
                new XElement(WsupXml.Service + "Xml", f.Xml)))
            .ToList();

        var locations = files
            .DistinctBy(f => f.DigestSha1)
            .Select(f => new XElement(WsupXml.Service + "FileLocation",
                new XElement(WsupXml.Service + "FileDigest", f.DigestSha1),
                new XElement(WsupXml.Service + "Url", f.SourceUrl)))
            .ToList();

        var xml = BuildExtendedUpdateInfo(wanted, locations, outOfScope);
        var result = new GetExtendedUpdateInfoResult(xml, requested.Count, wanted.Count, locations.Count, outOfScope.Count);

        DeviceUpdateMetrics.RecordExtendedInfo(started, result);
        return result;
    }

    private static bool FragmentWanted(string fragmentType, string language, HashSet<string> infoTypes, HashSet<string> locales)
    {
        if (infoTypes.Count > 0 && !infoTypes.Contains(fragmentType))
            return false;

        if (language.Length == 0 || locales.Count == 0)
            return true;

        return locales.Contains(language) || language.Equals("en", StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string> ParseStrings(string requestXml, string containerName)
    {
        var doc = XDocument.Parse(requestXml);
        var container = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == containerName);
        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (container is not null)
            foreach (var value in container.Elements())
                if (!string.IsNullOrWhiteSpace(value.Value))
                    values.Add(value.Value.Trim());

        return values;
    }

    private static HashSet<long> ParseRevisionIds(string requestXml)
    {
        var doc = XDocument.Parse(requestXml);
        var container = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "revisionIDs");
        var ids = new HashSet<long>();
        if (container is not null)
            foreach (var i in container.Elements().Where(e => e.Name.LocalName == "int"))
                if (long.TryParse(i.Value, out var value))
                    ids.Add(value);
        return ids;
    }

    private static string BuildExtendedUpdateInfo(
        List<XElement> updateElements,
        List<XElement> fileLocations,
        List<long> outOfScope)
    {
        XNamespace ns = WsupXml.ServiceNs;

        var result = new XElement(ns + "GetExtendedUpdateInfoResult",
            new XElement(ns + "Updates", updateElements),
            new XElement(ns + "FileLocations", fileLocations));

        if (outOfScope.Count > 0)
            result.Add(new XElement(ns + "OutOfScopeRevisionIDs",
                outOfScope.Select(id => new XElement(ns + "int", id))));

        return WsupXml.BuildResponse("GetExtendedUpdateInfoResponse", result);
    }
}
