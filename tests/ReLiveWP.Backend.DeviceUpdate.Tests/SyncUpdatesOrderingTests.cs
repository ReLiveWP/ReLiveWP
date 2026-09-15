using System.Text;
using System.Xml.Linq;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

// The order revisions come back in decides which 90 land in a batch, and therefore the whole paging
// sequence. Every other replay test compares sets, so none of them would notice it changing. Set
// DEVICEUPDATE_GOLDEN=write to re-record after a deliberate change.
[Collection(CaptureCollection.Name)]
public class SyncUpdatesOrderingTests(CaptureFixture captures)
{
    private static readonly string GoldenPath =
        Path.Combine(AppContext.BaseDirectory, "Golden", "sync-sequences.txt");

    private static List<long> OrderedIds(string responseXml, string section)
    {
        var container = XDocument.Parse(responseXml)
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == section);

        if (container is null)
            return [];

        return [.. container
            .Elements()
            .Where(e => e.Name.LocalName == "UpdateInfo")
            .Select(e => (long)e.Elements().First(c => c.Name.LocalName == "ID"))];
    }

    private async Task<string> DescribeRoundAsync(int round)
    {
        using var db = captures.NewContext();
        var result = await captures.NewService(db).SyncUpdatesAsync(captures.ReadRequest(round));

        var newIds = string.Join(",", OrderedIds(result.Xml, "NewUpdates"));
        var changedIds = string.Join(",", OrderedIds(result.Xml, "ChangedUpdates"));

        return $"{round}|truncated:{result.Truncated}|new:{newIds}|changed:{changedIds}";
    }

    private async Task<string> DescribeSessionAsync()
    {
        var lines = new StringBuilder();
        for (var round = CaptureFixture.FirstRound; round <= CaptureFixture.LastRound; round++)
            lines.AppendLine(await DescribeRoundAsync(round));

        return lines.ToString();
    }

    [Fact]
    public async Task EmissionOrderIsUnchanged()
    {
        var actual = await DescribeSessionAsync();

        if (Environment.GetEnvironmentVariable("DEVICEUPDATE_GOLDEN") == "write")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GoldenPath)!);
            await File.WriteAllTextAsync(GoldenPath, actual);
            return;
        }

        Assert.True(File.Exists(GoldenPath),
            $"no golden sequences at {GoldenPath}; re-record with DEVICEUPDATE_GOLDEN=write");

        var expected = await File.ReadAllTextAsync(GoldenPath);
        Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
    }
}
