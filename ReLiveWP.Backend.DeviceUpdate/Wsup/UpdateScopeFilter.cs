namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

public static class UpdateScopeFilter
{
    public const string BundleAction = "Bundle";

    public static HashSet<long> ResolveBundleReachable(
        IReadOnlyCollection<CandidateRevision> candidates,
        ILookup<long, Guid> bundledBy)
    {
        var byGuid = candidates
            .GroupBy(c => c.UpdateId)
            .ToDictionary(g => g.Key, g => g.First());

        var reachable = new HashSet<long>();
        var queue = new Queue<CandidateRevision>();

        foreach (var root in candidates.Where(c => c.DeploymentAction != BundleAction))
            queue.Enqueue(root);

        while (queue.Count > 0)
        {
            foreach (var child in bundledBy[queue.Dequeue().RevisionId])
            {
                if (!byGuid.TryGetValue(child, out var candidate))
                    continue;
                if (!reachable.Add(candidate.RevisionId))
                    continue;

                queue.Enqueue(candidate);
            }
        }

        return reachable;
    }
}
