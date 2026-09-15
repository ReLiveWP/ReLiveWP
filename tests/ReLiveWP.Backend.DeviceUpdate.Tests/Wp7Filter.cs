using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

// Reduces a full set of parsed updates to just the Windows Phone 7 catalog: the WP-relevant
// updates plus the minimal category/detectoid scaffolding they require. Everything else is discarded.
public static class Wp7Filter
{
    // True if an update is *directly* WP-relevant: the WP category itself, or anything whose
    // applicability rules query the mobile CSP (e.g. ./DevDetail/SwV). This is only the seed test;
    // WP membership also propagates transitively through the prerequisite graph (see IsWindowsPhone).
    public static bool IsDirectlyWindowsPhone(ParsedUpdate update)
    {
        if (update.UpdateId == WsupXml.WindowsPhoneCategoryId)
            return true;

        return update.SyncMetadataXml.Contains("MobileApplicabilityRules", StringComparison.Ordinal)
            || update.SyncMetadataXml.Contains("./DevDetail", StringComparison.Ordinal);
    }

    // Returns the subset of updates belonging to the WP7 catalog. An update is in the catalog if it
    // is directly WP-relevant or if any of its prerequisites is (transitively) WP — the WP category
    // sits at the root of every WP update's prerequisite chain, so membership flows up that chain
    // rather than only being visible one hop away. The kept set is then closed over prerequisites and
    // bundled updates so the scaffolding the client walks through (categories, detectoids, bundles,
    // bundle children) is retained even when an intermediate node is not itself WP-flagged.
    public static List<ParsedUpdate> Filter(IReadOnlyCollection<ParsedUpdate> updates)
    {
        var byGuid = updates
            .GroupBy(u => u.UpdateId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Memoized recursive test: a GUID is WP if any of its revisions is directly WP, or any of its
        // revisions lists a prerequisite GUID that is (recursively) WP. The visiting set breaks the
        // cycles that exist in the prerequisite graph (treating an in-progress node as not-yet-WP).
        var wp = new Dictionary<Guid, bool>();
        var visiting = new HashSet<Guid>();

        bool IsWindowsPhone(Guid guid)
        {
            if (wp.TryGetValue(guid, out var cached))
                return cached;
            if (!visiting.Add(guid))
                return false;
            if (!byGuid.TryGetValue(guid, out var revisions))
            {
                visiting.Remove(guid);
                return false;
            }

            var result = revisions.Any(r => IsDirectlyWindowsPhone(r)
                || r.Prerequisites.Any(p => IsWindowsPhone(p.PrerequisiteUpdateId)));

            visiting.Remove(guid);
            wp[guid] = result;
            return result;
        }

        var keptGuids = new HashSet<Guid>();
        var queue = new Queue<Guid>();

        foreach (var guid in byGuid.Keys)
            if (IsWindowsPhone(guid) && keptGuids.Add(guid))
                queue.Enqueue(guid);

        while (queue.Count > 0)
        {
            var guid = queue.Dequeue();
            if (!byGuid.TryGetValue(guid, out var revisions))
                continue;

            foreach (var revision in revisions)
            {
                foreach (var prereq in revision.Prerequisites)
                    if (keptGuids.Add(prereq.PrerequisiteUpdateId))
                        queue.Enqueue(prereq.PrerequisiteUpdateId);

                foreach (var bundle in revision.Bundles)
                    if (keptGuids.Add(bundle.BundledUpdateId))
                        queue.Enqueue(bundle.BundledUpdateId);
            }
        }

        return updates.Where(u => keptGuids.Contains(u.UpdateId)).ToList();
    }
}
