using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

public class SyncUpdatesParserTests
{
    private const string First = "a66daf6a-3f88-4f0e-a0d9-8881c3fddb09";
    private const string Second = "3d31b6da-2107-460a-8e97-6adb35a2ecd4";

    private static string Metadata(string relationships) =>
        $"""<UpdateIdentity UpdateID="c7219a12-44fa-457e-9083-05b97492320a" RevisionNumber="100" /><Properties UpdateType="Software" /><Relationships>{relationships}</Relationships>""";

    [Fact]
    public void ReadsBundleMembersListedDirectly()
    {
        var parsed = SyncUpdatesParser.ParseMetadataOnly(Metadata(
            $"""<BundledUpdates><UpdateIdentity UpdateID="{First}" RevisionNumber="100" /><UpdateIdentity UpdateID="{Second}" RevisionNumber="100" /></BundledUpdates>"""));

        Assert.Equal(2, parsed.Bundles.Count);
    }

    // MS-WUSP 3.1.1 gives BundledUpdates/AtLeastOne/UpdateIdentity as the canonical shape. Missing it
    // silently dropped 219 child edges across the real catalog, which orphaned whole language rollups.
    [Fact]
    public void ReadsBundleMembersNestedInAtLeastOne()
    {
        var parsed = SyncUpdatesParser.ParseMetadataOnly(Metadata(
            $"""<BundledUpdates><AtLeastOne><UpdateIdentity UpdateID="{First}" RevisionNumber="100" /><UpdateIdentity UpdateID="{Second}" RevisionNumber="100" /></AtLeastOne></BundledUpdates>"""));

        Assert.Equal(2, parsed.Bundles.Count);
        Assert.Contains(parsed.Bundles, b => b.BundledUpdateId == Guid.Parse(First));
        Assert.Contains(parsed.Bundles, b => b.BundledUpdateId == Guid.Parse(Second));
    }

    [Fact]
    public void PrerequisiteGroupsSeparateMandatoryFromAtLeastOne()
    {
        var parsed = SyncUpdatesParser.ParseMetadataOnly(Metadata(
            $"""<Prerequisites><UpdateIdentity UpdateID="{First}" /><AtLeastOne><UpdateIdentity UpdateID="{Second}" /></AtLeastOne></Prerequisites>"""));

        Assert.Equal(0, parsed.Prerequisites.Single(p => p.PrerequisiteUpdateId == Guid.Parse(First)).GroupId);
        Assert.True(parsed.Prerequisites.Single(p => p.PrerequisiteUpdateId == Guid.Parse(Second)).GroupId > 0);
    }
}
