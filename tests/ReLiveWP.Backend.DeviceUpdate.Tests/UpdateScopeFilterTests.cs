using ReLiveWP.Backend.DeviceUpdate.Model;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

public class UpdateScopeFilterTests
{
    private static Guid Id(int n) => new(n, 0, 0, [0, 0, 0, 0, 0, 0, 0, 0]);

    private static CandidateRevision Make(int n, string action) =>
        new(n, Id(n), 1, UpdateType.Software, true, action, null);

    private static HashSet<long> Resolve(List<CandidateRevision> candidates, params (int Parent, int Child)[] edges)
    {
        var bundledBy = edges.ToLookup(e => (long)e.Parent, e => Id(e.Child));
        return UpdateScopeFilter.ResolveBundleReachable(candidates, bundledBy);
    }

    [Fact]
    public void BundledRevisionWithNoParentIsNotReachable()
    {
        List<CandidateRevision> candidates = [Make(1, "Bundle")];

        Assert.Empty(Resolve(candidates));
    }

    [Fact]
    public void BundledRevisionIsReachableThroughAnInstallParent()
    {
        List<CandidateRevision> candidates = [Make(1, "Install"), Make(2, "Bundle")];

        Assert.Equal([2L], Resolve(candidates, (1, 2)));
    }

    // A bundle can be bundled by another bundle, so one hop is not enough.
    [Fact]
    public void ReachabilityFollowsChainsOfBundles()
    {
        List<CandidateRevision> candidates = [Make(1, "Install"), Make(2, "Bundle"), Make(3, "Bundle")];

        Assert.Equal([2L, 3L], Resolve(candidates, (1, 2), (2, 3)).Order());
    }

    [Fact]
    public void ReachabilityTerminatesOnCycles()
    {
        List<CandidateRevision> candidates = [Make(1, "Install"), Make(2, "Bundle"), Make(3, "Bundle")];

        Assert.Equal([2L, 3L], Resolve(candidates, (1, 2), (2, 3), (3, 2)).Order());
    }
}
