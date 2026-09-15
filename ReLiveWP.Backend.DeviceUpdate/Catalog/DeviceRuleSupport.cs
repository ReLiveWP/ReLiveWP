namespace ReLiveWP.Backend.DeviceUpdate.Catalog;

// What DuaSvc.dll can actually evaluate. Authoring a rule outside this set is worse than authoring a
// rule that returns false: an unknown element fails the whole update with 0xC00CEE00 and parks it in
// status 2 EvaluationFailed, and a comparison token outside the right vocabulary is swallowed to
// false, which reads as a targeting bug rather than a manifest bug. Both are caught here instead.
public static class DeviceRuleSupport
{
    private const string RomPackagePrefix = "./Vendor/MSFT/ROMPackage/";
    private const string PkgVersionLeaf = "/PkgVersion";

    // DuaSvc checks the PkgVersion shape by total length, so anything else drops to the generic
    // ConfigManager path and compares the packed u64 as a string.
    private const int PkgVersionUriLength = 72;

    // Scalar comparisons, used by CspQuery and b.RegDword. Case sensitive on the device.
    private static readonly string[] Scalar =
        ["EqualTo", "LessThan", "LessThanOrEqualTo", "GreaterThanOrEqualTo", "GreaterThan"];

    // b.RegSz has no ordering operators at all. Asking for one is the silent-false case.
    private static readonly string[] Text = ["EqualTo", "BeginsWith", "Contains", "EndsWith"];

    private const int MaxCspValue = 260;
    private const int MaxRegSzData = 0x3FFF;

    public static string PkgVersionUri(Guid package) =>
        $"{RomPackagePrefix}{package:D}{PkgVersionLeaf}";

    public static string PackageStateUri(Guid package) =>
        $"{RomPackagePrefix}{package:D}/State";

    // CPkgInfoCSP hardcodes State to the string "80"; it is not a real state, just a node that reads
    // back only for an installed package.
    public const string InstalledPackageState = "80";

    public static void Validate(ApplicabilityRule rule)
    {
        switch (rule)
        {
            case ApplicabilityRule.RegSz r:
                RequireComparison(r.Comparison, Text, "b.RegSz");
                RequireLength(r.Data, MaxRegSzData, "b.RegSz Data");
                break;

            case ApplicabilityRule.RegDword r:
                RequireComparison(r.Comparison, Scalar, "b.RegDword");
                break;

            case ApplicabilityRule.CspQuery r:
                RequireComparison(r.Comparison, Scalar, "CspQuery");
                RequireLength(r.Value, MaxCspValue, "CspQuery Value");
                RequireWellFormedRomPackageUri(r.LocUri);
                break;

            case ApplicabilityRule.PackageVersion r:
                RequireComparison(r.Comparison, Scalar, "CspQuery");
                RequireFourPartVersion(r.Value);
                RequireLength(PkgVersionUri(r.Package), PkgVersionUriLength, "PkgVersion LocUri", exact: true);
                break;

            case ApplicabilityRule.PackageInstalled:
                break;

            case ApplicabilityRule.All r:
                foreach (var child in r.Rules)
                    Validate(child);
                break;

            case ApplicabilityRule.Any r:
                foreach (var child in r.Rules)
                    Validate(child);
                break;

            case ApplicabilityRule.Negated r:
                Validate(r.Rule);
                break;

            default:
                throw new InvalidOperationException(
                    $"{rule.GetType().Name} has no evaluator on the device; it would fail the update rather than return false.");
        }
    }

    private static void RequireComparison(string comparison, string[] allowed, string element)
    {
        if (!allowed.Contains(comparison, StringComparer.Ordinal))
            throw new InvalidOperationException(
                $"{element} cannot compare with '{comparison}'. The device accepts {string.Join(", ", allowed)}, " +
                "case sensitively, and swallows anything else to false.");
    }

    private static void RequireFourPartVersion(string value)
    {
        var parts = value.Split('.');
        if (parts.Length != 4 || !parts.All(p => ushort.TryParse(p, out _)))
            throw new InvalidOperationException(
                $"PkgVersion needs exactly four numeric components, got '{value}'. Three parses everywhere else " +
                "but not here, and a failed parse degrades the comparison to a string compare.");
    }

    // A hand-written ROMPackage LocUri that is the wrong length, or carries a braced guid, reads back
    // as a decimal u64 string and compares lexicographically instead of as a version.
    private static void RequireWellFormedRomPackageUri(string locUri)
    {
        if (!locUri.StartsWith(RomPackagePrefix, StringComparison.Ordinal))
            return;

        if (locUri.Contains('{') || locUri.Contains('}'))
            throw new InvalidOperationException(
                $"'{locUri}' has a braced package guid. ConvertGuidBraceFormat adds braces unconditionally, so this never resolves.");

        if (locUri.EndsWith(PkgVersionLeaf, StringComparison.Ordinal) && locUri.Length != PkgVersionUriLength)
            throw new InvalidOperationException(
                $"'{locUri}' is {locUri.Length} characters; the device only takes the PkgVersion path at exactly " +
                $"{PkgVersionUriLength}. Use a PackageVersion rule rather than spelling the LocUri.");
    }

    private static void RequireLength(string value, int limit, string what, bool exact = false)
    {
        if (exact ? value.Length != limit : value.Length > limit)
            throw new InvalidOperationException($"{what} is {value.Length} characters, expected {(exact ? "" : "at most ")}{limit}.");
    }
}
