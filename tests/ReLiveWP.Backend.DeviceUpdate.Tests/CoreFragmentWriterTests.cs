using ReLiveWP.Backend.DeviceUpdate.Catalog;
using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

// The expected strings are real Core fragments lifted out of the crawled catalog. An authored
// fragment the WP7 parser has never seen a shape of is the one thing no replay test can catch, so
// these pin the writer against upstream's own bytes.
public class CoreFragmentWriterTests
{
    [Fact]
    public void ReproducesACrawledDeviceDetectoidByteForByte()
    {
        var update = new ManifestUpdate
        {
            UpdateId = new Guid("1f6498a5-9106-4612-94e1-fab7479dcb03"),
            RevisionNumber = 100,
            UpdateType = UpdateType.Detectoid,
            RequiredUpdateIds = [new Guid("c961cb92-2bfa-4f8c-92ae-8371471d06ce")],
            IsInstalled = new ApplicabilityRule.RegSz(
                @"System\Platform\DeviceTargetingInfo", "OEMDeviceName", "EqualTo", "RM-819_SE_381"),
        };

        const string expected =
            """<UpdateIdentity UpdateID="1f6498a5-9106-4612-94e1-fab7479dcb03" RevisionNumber="100" /><Properties UpdateType="Detectoid" /><Relationships><Prerequisites><UpdateIdentity UpdateID="c961cb92-2bfa-4f8c-92ae-8371471d06ce" /></Prerequisites></Relationships><ApplicabilityRules><IsInstalled><b.RegSz Key="HKEY_LOCAL_MACHINE" Subkey="System\Platform\DeviceTargetingInfo" Value="OEMDeviceName" Comparison="EqualTo" Data="RM-819_SE_381" xmlns="http://schemas.microsoft.com/msus/2002/12/BaseApplicabilityRules" /></IsInstalled></ApplicabilityRules>""";

        Assert.Equal(expected, CoreFragmentWriter.Write(update));
    }

    // Every WP7 rule element in the catalog redeclares its own default namespace, including the
    // second child of an And where the declaration is already in scope one level up. 1324 RegSz and
    // 1632 CspQuery carry it, none omit it.
    [Fact]
    public void RedeclaresTheRuleNamespaceOnEveryNestedRule()
    {
        var swv = "./DevDetail/SwV";
        var update = new ManifestUpdate
        {
            UpdateId = new Guid("2db421eb-77e3-41b3-a404-b48355ae8372"),
            RevisionNumber = 100,
            UpdateType = UpdateType.Software,
            IsInstalled = new ApplicabilityRule.CspQuery(swv, "GreaterThanOrEqualTo", "7.10.7720.68"),
            IsSuperseded = new ApplicabilityRule.All([
                new ApplicabilityRule.CspQuery(swv, "GreaterThan", "7.0.7403.0"),
                new ApplicabilityRule.CspQuery(swv, "LessThan", "7.10.7720.68"),
            ]),
            IsInstallable = new ApplicabilityRule.CspQuery(swv, "LessThanOrEqualTo", "7.0.7403.0"),
        };

        const string expected =
            """<ApplicabilityRules><IsInstalled><CspQuery LocUri="./DevDetail/SwV" Comparison="GreaterThanOrEqualTo" Value="7.10.7720.68" xmlns="http://schemas.microsoft.com/msus/2002/12/MobileApplicabilityRules" /></IsInstalled><IsSuperseded><And xmlns="http://schemas.microsoft.com/msus/2002/12/LogicalApplicabilityRules"><CspQuery LocUri="./DevDetail/SwV" Comparison="GreaterThan" Value="7.0.7403.0" xmlns="http://schemas.microsoft.com/msus/2002/12/MobileApplicabilityRules" /><CspQuery LocUri="./DevDetail/SwV" Comparison="LessThan" Value="7.10.7720.68" xmlns="http://schemas.microsoft.com/msus/2002/12/MobileApplicabilityRules" /></And></IsSuperseded><IsInstallable><CspQuery LocUri="./DevDetail/SwV" Comparison="LessThanOrEqualTo" Value="7.0.7403.0" xmlns="http://schemas.microsoft.com/msus/2002/12/MobileApplicabilityRules" /></IsInstallable></ApplicabilityRules>""";

        var written = CoreFragmentWriter.Write(update);

        Assert.Contains(expected, written);
    }

    [Fact]
    public void MarksCategoryChoicesAndLeavesPlainOnesAlone()
    {
        var update = new ManifestUpdate
        {
            UpdateId = new Guid("aaaaaaaa-0000-0000-0000-000000000001"),
            UpdateType = UpdateType.Software,
            Choices =
            [
                new ManifestChoice { UpdateIds = [new Guid("b2ba61f0-0e23-4fd3-946e-0f5abc1de1b8")], IsCategory = true },
                new ManifestChoice { UpdateIds = [new Guid("aaaaaaaa-0000-0000-0000-000000000002")] },
            ],
        };

        var written = CoreFragmentWriter.Write(update);

        Assert.Contains("""<AtLeastOne IsCategory="true"><UpdateIdentity UpdateID="b2ba61f0-0e23-4fd3-946e-0f5abc1de1b8" /></AtLeastOne>""", written);
        Assert.Contains("""<AtLeastOne><UpdateIdentity UpdateID="aaaaaaaa-0000-0000-0000-000000000002" /></AtLeastOne>""", written);
    }

    // The fragment is round-tripped by the same parser that reads crawled metadata, which is what
    // makes an authored row indistinguishable from a crawled one downstream.
    [Fact]
    public void RoundTripsThroughTheCrawlerMetadataParser()
    {
        var anchor = new Guid("b2ba61f0-0e23-4fd3-946e-0f5abc1de1b8");
        var armv7 = new Guid("68cb93ff-3840-4b11-a303-6c462508d87b");
        var update = new ManifestUpdate
        {
            UpdateId = new Guid("aaaaaaaa-0000-0000-0000-000000000003"),
            RevisionNumber = 7,
            UpdateType = UpdateType.Detectoid,
            RequiredUpdateIds = [armv7],
            Choices = [new ManifestChoice { UpdateIds = [anchor], IsCategory = true }],
            IsInstalled = new ApplicabilityRule.RegSz(@"Software\ReLiveWP\Platform", "Installed", "EqualTo", "1"),
        };

        var parsed = DeviceUpdate.Wsup.SyncUpdatesParser.ParseMetadataOnly(CoreFragmentWriter.Write(update));

        Assert.Equal(update.UpdateId, parsed.UpdateId);
        Assert.Equal(7, parsed.RevisionNumber);
        Assert.Equal(UpdateType.Detectoid, parsed.UpdateType);

        var mandatory = Assert.Single(parsed.Prerequisites, p => p.GroupId == 0);
        Assert.Equal(armv7, mandatory.PrerequisiteUpdateId);

        var choice = Assert.Single(parsed.Prerequisites, p => p.GroupId > 0);
        Assert.Equal(anchor, choice.PrerequisiteUpdateId);
        Assert.True(choice.IsCategory);
    }
}
