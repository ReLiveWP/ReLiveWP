using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

[Collection(CaptureCollection.Name)]
public class SyncUpdatesReplayTests(CaptureFixture captures)
{
    private const int BatchSize = 90;

    public static TheoryData<int> Rounds()
    {
        var data = new TheoryData<int>();
        for (var round = CaptureFixture.FirstRound; round <= CaptureFixture.LastRound; round++)
            data.Add(round);
        return data;
    }

    private async Task<string> ReplayAsync(int round)
    {
        using var db = captures.NewContext();
        var result = await captures.NewService(db).SyncUpdatesAsync(captures.ReadRequest(round));
        return result.Xml;
    }

    [Theory]
    [MemberData(nameof(Rounds))]
    public async Task ServesOnlyRevisionsTheCatalogHolds(int round)
    {
        var response = await ReplayAsync(round);
        var known = await captures.KnownRevisionIdsAsync();

        var served = CaptureFixture.UpdateInfoIds(response, "NewUpdates");
        served.UnionWith(CaptureFixture.UpdateInfoIds(response, "ChangedUpdates"));

        Assert.Empty(served.Except(known));
    }

    // We page the catalog in our own order, so a round-for-round comparison is meaningless. What
    // must hold is that nothing we ever offer is something upstream considered out of scope.
    private HashSet<long> UpstreamServedAcrossSession()
    {
        var all = new HashSet<long>();
        for (var round = CaptureFixture.FirstRound; round <= CaptureFixture.LastRound; round++)
        {
            var upstream = captures.ReadResponse(round);
            all.UnionWith(CaptureFixture.UpdateInfoIds(upstream, "NewUpdates"));
            all.UnionWith(CaptureFixture.UpdateInfoIds(upstream, "ChangedUpdates"));
        }
        return all;
    }

    [Theory]
    [MemberData(nameof(Rounds))]
    public async Task NeverOffersSomethingUpstreamWithheld(int round)
    {
        var response = await ReplayAsync(round);
        var theirs = UpstreamServedAcrossSession();

        var ours = CaptureFixture.UpdateInfoIds(response, "NewUpdates");
        ours.UnionWith(CaptureFixture.UpdateInfoIds(response, "ChangedUpdates"));

        Assert.Empty(ours.Except(theirs));
    }

    [Theory]
    [MemberData(nameof(Rounds))]
    public async Task NewUpdatesExcludesWhatTheClientAlreadyCached(int round)
    {
        var request = captures.ReadRequest(round);
        var response = await ReplayAsync(round);

        var cached = CaptureFixture.ReportedIds(request, "InstalledNonLeafUpdateIDs");
        cached.UnionWith(CaptureFixture.ReportedIds(request, "OtherCachedUpdateIDs"));

        Assert.Empty(CaptureFixture.UpdateInfoIds(response, "NewUpdates").Intersect(cached));
    }

    [Theory]
    [MemberData(nameof(Rounds))]
    public async Task ChangedUpdatesIsExactlyTheCachedPortionOfNeeded(int round)
    {
        var request = captures.ReadRequest(round);
        var response = await ReplayAsync(round);

        var cached = CaptureFixture.ReportedIds(request, "InstalledNonLeafUpdateIDs");
        cached.UnionWith(CaptureFixture.ReportedIds(request, "OtherCachedUpdateIDs"));

        var changed = CaptureFixture.UpdateInfoIds(response, "ChangedUpdates");

        Assert.Empty(changed.Except(cached));
    }

    [Theory]
    [MemberData(nameof(Rounds))]
    public async Task ChangedUpdatesCarryNoMetadata(int round)
    {
        var response = await ReplayAsync(round);

        var changed = XDocument.Parse(response)
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ChangedUpdates");

        Assert.NotNull(changed);
        Assert.DoesNotContain(changed.Descendants(), e => e.Name.LocalName == "Xml");
    }

    [Theory]
    [MemberData(nameof(Rounds))]
    public async Task TruncatedIsSetOnlyWhenTheBatchIsFull(int round)
    {
        var response = await ReplayAsync(round);

        var count = CaptureFixture.UpdateInfoIds(response, "NewUpdates").Count;
        var truncated = (bool)XDocument.Parse(response)
            .Descendants()
            .First(e => e.Name.LocalName == "Truncated");

        Assert.True(count <= BatchSize);
        if (truncated)
            Assert.Equal(BatchSize, count);
    }

    [Fact]
    public async Task PagingTerminatesOnceEverythingOfferedIsCached()
    {
        var response = await ReplayAsync(CaptureFixture.LastRound);

        Assert.Empty(CaptureFixture.UpdateInfoIds(response, "NewUpdates"));
        Assert.False((bool)XDocument.Parse(response)
            .Descendants()
            .First(e => e.Name.LocalName == "Truncated"));
    }

    // On the last round the client has cached everything, so ChangedUpdates carries the whole Needed
    // set and the two sides are directly comparable. Any gap must be explained by the catalog rather
    // than by the Needed computation; once the catalog resolves every reference the gap is empty.
    [Fact]
    public async Task WithheldRevisionsAreOnesTheCatalogCannotSatisfy()
    {
        using var db = captures.NewContext();
        var report = await CatalogVerifier.CheckCatalogAsync(db);
        var unsatisfiable = report.UnsatisfiableRevisions.ToHashSet();
        var known = await captures.KnownRevisionIdsAsync();

        var response = await ReplayAsync(CaptureFixture.LastRound);
        var upstream = captures.ReadResponse(CaptureFixture.LastRound);

        var theirs = CaptureFixture.UpdateInfoIds(upstream, "ChangedUpdates").Intersect(known);
        var ours = CaptureFixture.UpdateInfoIds(response, "ChangedUpdates");

        Assert.Empty(theirs.Except(ours).Except(unsatisfiable));
    }
}
