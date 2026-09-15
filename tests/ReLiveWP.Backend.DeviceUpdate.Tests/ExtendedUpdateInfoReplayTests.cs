using System.Xml.Linq;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

// Fragment order inside a revision is not derivable from the data: every stored Ordinal used to be
// 0, so the wire order was whatever EF happened to load. These pin it against what upstream sent.
[Collection(CaptureCollection.Name)]
public class ExtendedUpdateInfoReplayTests(CaptureFixture captures)
{
    private const int Round = 1;

    private static List<(long Revision, string Fragment)> FragmentSequence(string xml)
    {
        var updates = XDocument.Parse(xml)
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "Updates");

        if (updates is null)
            return [];

        return [.. updates
            .Elements()
            .Where(e => e.Name.LocalName == "Update")
            .Select(e =>
            {
                var id = (long)e.Elements().First(c => c.Name.LocalName == "ID");
                var body = e.Elements().First(c => c.Name.LocalName == "Xml").Value;
                return (id, RootElementName(body));
            })];
    }

    private static string RootElementName(string fragmentXml)
    {
        var first = WsupXml.ParseFragment(fragmentXml).Elements().FirstOrDefault();
        if (first is null)
            return "";

        var language = first.Elements().FirstOrDefault(e => e.Name.LocalName == "Language")?.Value;
        return language is null ? first.Name.LocalName : $"{first.Name.LocalName}:{language}";
    }

    private async Task<string> ReplayAsync()
    {
        using var db = captures.NewContext();
        var result = await captures.NewService(db).GetExtendedUpdateInfoAsync(captures.ReadExtendedRequest(Round));
        return result.Xml;
    }

    [Fact]
    public async Task ServesTheSameFragmentsUpstreamDid()
    {
        var ours = FragmentSequence(await ReplayAsync());
        var theirs = FragmentSequence(captures.ReadExtendedResponse(Round));

        Assert.Equal(theirs.Order(), ours.Order());
    }

    // The fixture imports the captured response, so this asserts we re-emit a revision's fragments in
    // the order we ingested them. It is not evidence that a crawled catalog matches a live fe2
    // response: upstream's locale order is stable per revision but we do not reproduce it.
    [Fact]
    public async Task PreservesTheFragmentOrderItIngested()
    {
        var ours = FragmentSequence(await ReplayAsync());
        var theirs = FragmentSequence(captures.ReadExtendedResponse(Round));

        foreach (var revision in theirs.Select(f => f.Revision).Distinct())
            Assert.Equal(
                theirs.Where(f => f.Revision == revision).Select(f => f.Fragment),
                ours.Where(f => f.Revision == revision).Select(f => f.Fragment));
    }

    [Fact]
    public async Task ExtendedFragmentComesFirstForEachRevision()
    {
        var ours = FragmentSequence(await ReplayAsync());

        foreach (var group in ours.GroupBy(f => f.Revision))
            Assert.StartsWith("ExtendedProperties", group.First().Fragment, StringComparison.Ordinal);
    }
}
