using ReLiveWP.Backend.DeviceUpdate.Catalog;
using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

// Everything here is a rule the device accepts and then gets wrong, or rejects in a way that parks
// the whole update in EvaluationFailed. None of it shows up as a serving bug, so the manifest is the
// only place it can be caught.
public class DeviceRuleSupportTests
{
    private static readonly Guid Package = new("3752d1fb-94e8-47ed-8302-e3fe267fd6e6");

    private static string WriteRule(ApplicabilityRule rule) =>
        CoreFragmentWriter.Write(new ManifestUpdate
        {
            UpdateId = Guid.Empty,
            UpdateType = UpdateType.Detectoid,
            IsInstalled = rule,
        });

    // DuaSvc takes the real 64-bit version path only when the LocUri is exactly 72 characters.
    [Fact]
    public void PackageVersionUriIsTheLengthTheDeviceChecksFor()
    {
        Assert.Equal(72, DeviceRuleSupport.PkgVersionUri(Package).Length);
        Assert.Equal(
            "./Vendor/MSFT/ROMPackage/3752d1fb-94e8-47ed-8302-e3fe267fd6e6/PkgVersion",
            DeviceRuleSupport.PkgVersionUri(Package));
    }

    [Fact]
    public void PackageInstalledWritesTheStatePredicate()
    {
        var written = WriteRule(new ApplicabilityRule.PackageInstalled(Package));

        Assert.Contains(
            """<CspQuery LocUri="./Vendor/MSFT/ROMPackage/3752d1fb-94e8-47ed-8302-e3fe267fd6e6/State" Comparison="EqualTo" Value="80" """,
            written);
    }

    [Fact]
    public void PackageVersionComparesAsAVersion()
    {
        var written = WriteRule(new ApplicabilityRule.PackageVersion(Package, "GreaterThanOrEqualTo", "7.10.8162.218"));

        Assert.Contains(
            """<CspQuery LocUri="./Vendor/MSFT/ROMPackage/3752d1fb-94e8-47ed-8302-e3fe267fd6e6/PkgVersion" Comparison="GreaterThanOrEqualTo" Value="7.10.8162.218" """,
            written);
    }

    // Three components parse everywhere else on the device but not here, and the failed parse
    // degrades the comparison to wcsicmp rather than erroring.
    [Fact]
    public void RejectsAThreePartPackageVersion()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            WriteRule(new ApplicabilityRule.PackageVersion(Package, "EqualTo", "7.10.8162")));

        Assert.Contains("exactly four", error.Message);
    }

    // b.RegSz has no ordering operators; the device swallows the bad token to false.
    [Fact]
    public void RejectsAnOrderingComparisonOnARegistryString()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            WriteRule(new ApplicabilityRule.RegSz(@"Software\ReLiveWP", "Version", "GreaterThanOrEqualTo", "1")));

        Assert.Contains("BeginsWith", error.Message);
    }

    [Fact]
    public void AcceptsTheStringComparisonsTheDeviceDoesImplement()
    {
        foreach (var comparison in (string[])["EqualTo", "BeginsWith", "Contains", "EndsWith"])
            WriteRule(new ApplicabilityRule.RegSz(@"Software\ReLiveWP", "Version", comparison, "1"));
    }

    [Fact]
    public void RejectsAnUnknownScalarComparison()
    {
        Assert.Throws<InvalidOperationException>(() =>
            WriteRule(new ApplicabilityRule.CspQuery("./DevDetail/SwV", "NotEqualTo", "7.10.8858.136")));
    }

    [Fact]
    public void RejectsABracedPackageGuidInAHandWrittenLocUri()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            WriteRule(new ApplicabilityRule.CspQuery(
                "./Vendor/MSFT/ROMPackage/{3752d1fb-94e8-47ed-8302-e3fe267fd6e6}/PkgVersion", "EqualTo", "7.10.8162.218")));

        Assert.Contains("braced", error.Message);
    }

    [Fact]
    public void RejectsAHandWrittenPackageVersionUriOfTheWrongLength()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            WriteRule(new ApplicabilityRule.CspQuery(
                "./Vendor/MSFT/ROMPackage/3752d1fb-94e8-47ed-8302-e3fe267fd6e6/x/PkgVersion", "EqualTo", "7.10.8162.218")));

        Assert.Contains("72", error.Message);
    }

    [Fact]
    public void ValidatesRulesNestedInsideLogicalOperators()
    {
        Assert.Throws<InvalidOperationException>(() =>
            WriteRule(new ApplicabilityRule.Negated(
                new ApplicabilityRule.PackageVersion(Package, "EqualTo", "7.10.8162"))));
    }
}
