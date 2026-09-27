using ReLiveWP.Backend.DeviceUpdate.Wsup;
using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Catalog;

// The authored half of the catalog, as it is written in Catalog/*.xml and before any ids are
// allocated. Compiled into the same ParsedUpdate the crawler produces, never into entities directly.

public class ManifestUpdate
{
    public Guid UpdateId { get; set; }
    public string Slug { get; set; } = "";
    public int RevisionNumber { get; set; } = 1;
    public UpdateType UpdateType { get; set; } = UpdateType.Software;
    public string DeploymentAction { get; set; } = "Install";
    public bool ExplicitlyDeployable { get; set; }

    // MAY be a leaf, this can be overriden 
    public bool IsLeaf { get; set; } = true;

    public List<Guid> RequiredUpdateIds { get; set; } = [];
    public List<ManifestChoice> Choices { get; set; } = [];
    public List<Guid> BundledUpdateIds { get; set; } = [];

    public ApplicabilityRule? IsInstalled { get; set; }
    public ApplicabilityRule? IsSuperseded { get; set; }
    public ApplicabilityRule? IsInstallable { get; set; }

    public List<ManifestFile> Files { get; set; } = [];
    public List<ManifestLocalization> Localizations { get; set; } = [];
    public ManifestInstallCommand? InstallCommand { get; set; }
}

public class ManifestOsUpgradeBlock
{
    public DeviceRing Ring { get; set; } = DeviceRing.ReLiveWP;
    public Version MinimumOsVersion { get; set; } = new(0, 0, 0, 0);
    public string Action { get; set; } = UpdateService.BlockAction;
    public string? Reason { get; set; }
}

public class ManifestChoice
{
    public List<Guid> UpdateIds { get; set; } = [];
    public bool IsCategory { get; set; }
}

public class ManifestFile
{
    public string Path { get; set; } = "";
}

public class ManifestLocalization
{
    public string Language { get; set; } = "en";
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? MoreInfoUrl { get; set; }
    public string? SupportUrl { get; set; }
}

public class ManifestInstallCommand
{
    public string Program { get; set; } = "IU";
    public string? Arguments { get; set; }
    public string DefaultResult { get; set; } = "Failed";
    public bool RebootByDefault { get; set; }
}

public abstract record ApplicabilityRule
{
    public sealed record RegSz(string Subkey, string Value, string Comparison, string Data) : ApplicabilityRule;

    public sealed record RegDword(string Subkey, string Value, string Comparison, int Data) : ApplicabilityRule;

    public sealed record CspQuery(string LocUri, string Comparison, string Value) : ApplicabilityRule;

    public sealed record PackageVersion(Guid Package, string Comparison, string Value) : ApplicabilityRule;

    public sealed record PackageInstalled(Guid Package) : ApplicabilityRule;

    public sealed record All(IReadOnlyList<ApplicabilityRule> Rules) : ApplicabilityRule;

    public sealed record Any(IReadOnlyList<ApplicabilityRule> Rules) : ApplicabilityRule;

    public sealed record Negated(ApplicabilityRule Rule) : ApplicabilityRule;
}
