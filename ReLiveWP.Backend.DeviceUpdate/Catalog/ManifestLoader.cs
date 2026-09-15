using System.Xml.Linq;
using ReLiveWP.Backend.DeviceUpdate.Wsup;
using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Catalog;

public record CatalogManifest(List<ManifestUpdate> Updates, List<ManifestOsUpgradeBlock> OsUpgradeBlocks);

public static class ManifestLoader
{
    public static CatalogManifest Load(string path)
    {
        if (!Directory.Exists(path))
            return new CatalogManifest([], []);

        var blocks = new List<ManifestOsUpgradeBlock>();
        foreach (var file in Directory.EnumerateFiles(path, "*.xml").Order())
            blocks.AddRange(XDocument.Load(file).Root!
                .Elements("Deployments")
                .Elements("BlockOsUpgrade")
                .Select(ReadOsUpgradeBlock));

        return new CatalogManifest(LoadDirectory(path), blocks);
    }

    private static ManifestOsUpgradeBlock ReadOsUpgradeBlock(XElement element) => new()
    {
        Ring = Enum.Parse<DeviceRing>((string?)element.Attribute("ring") ?? nameof(DeviceRing.ReLiveWP)),
        MinimumOsVersion = Version.Parse(Required(element, "minimumOsVersion")),
        Action = (string?)element.Attribute("action") ?? UpdateService.BlockAction,
        Reason = (string?)element.Attribute("reason"),
    };

    public static List<ManifestUpdate> LoadDirectory(string path)
    {
        if (!Directory.Exists(path))
            return [];

        var updates = new List<ManifestUpdate>();
        foreach (var file in Directory.EnumerateFiles(path, "*.xml").Order())
            updates.AddRange(LoadFile(file));

        var duplicate = updates
            .GroupBy(u => (u.UpdateId, u.RevisionNumber))
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
            throw new InvalidOperationException(
                $"Update {duplicate.Key.UpdateId} revision {duplicate.Key.RevisionNumber} is declared more than once.");

        return updates;
    }

    public static List<ManifestUpdate> LoadFile(string path) =>
        [.. XDocument.Load(path).Root!.Elements("Update").Select(ReadUpdate)];

    private static ManifestUpdate ReadUpdate(XElement element)
    {
        var update = new ManifestUpdate
        {
            UpdateId = Guid.Parse(Required(element, "id")),
            Slug = Required(element, "slug"),
            RevisionNumber = (int?)element.Attribute("revision") ?? 1,
            UpdateType = Enum.Parse<UpdateType>((string?)element.Attribute("type") ?? "Software"),
            DeploymentAction = (string?)element.Attribute("action") ?? "Install",
            ExplicitlyDeployable = (bool?)element.Attribute("explicitlyDeployable") ?? false,
            IsLeaf = (bool?)element.Attribute("isLeaf") ?? true,
        };

        foreach (var prerequisites in element.Elements("Prerequisites"))
        {
            update.RequiredUpdateIds.AddRange(prerequisites.Elements("Update").Select(ReadReference));

            update.Choices.AddRange(prerequisites.Elements("AnyOf").Select(choice => new ManifestChoice
            {
                UpdateIds = [.. choice.Elements("Update").Select(ReadReference)],
                IsCategory = (bool?)choice.Attribute("isCategory") ?? false,
            }));
        }

        foreach (var bundled in element.Elements("BundledUpdates"))
            update.BundledUpdateIds.AddRange(bundled.Elements("Update").Select(ReadReference));

        update.IsInstalled = ReadRuleHolder(element, "IsInstalled");
        update.IsSuperseded = ReadRuleHolder(element, "IsSuperseded");
        update.IsInstallable = ReadRuleHolder(element, "IsInstallable");

        update.Files = [.. element.Elements("Files").Elements("File")
            .Select(f => new ManifestFile { Path = Required(f, "path") })];

        update.Localizations = [.. element.Elements("Localization").Select(l => new ManifestLocalization
        {
            Language = (string?)l.Attribute("lang") ?? "en",
            Title = (string?)l.Element("Title"),
            Description = (string?)l.Element("Description"),
            MoreInfoUrl = (string?)l.Element("MoreInfoUrl"),
            SupportUrl = (string?)l.Element("SupportUrl"),
        })];

        if (element.Element("InstallCommand") is { } install)
            update.InstallCommand = new ManifestInstallCommand
            {
                Program = (string?)install.Attribute("program") ?? "IU",
                Arguments = (string?)install.Attribute("arguments"),
                DefaultResult = (string?)install.Attribute("defaultResult") ?? "Failed",
                RebootByDefault = (bool?)install.Attribute("rebootByDefault") ?? false,
            };

        return update;
    }

    private static Guid ReadReference(XElement element) => Guid.Parse(Required(element, "id"));

    private static ApplicabilityRule? ReadRuleHolder(XElement update, string name)
    {
        var holder = update.Element(name);
        if (holder is null)
            return null;

        var rules = holder.Elements().Select(ReadRule).ToList();
        return rules.Count switch
        {
            0 => throw new InvalidOperationException($"<{name}> on {update.Attribute("slug")?.Value} has no rule."),
            1 => rules[0],
            _ => new ApplicabilityRule.All(rules),
        };
    }

    private static ApplicabilityRule ReadRule(XElement element) => element.Name.LocalName switch
    {
        "RegSz" => new ApplicabilityRule.RegSz(
            Required(element, "subkey"), Required(element, "value"),
            (string?)element.Attribute("comparison") ?? "EqualTo", Required(element, "data")),

        "RegDword" => new ApplicabilityRule.RegDword(
            Required(element, "subkey"), Required(element, "value"),
            (string?)element.Attribute("comparison") ?? "EqualTo", (int)element.Attribute("data")!),

        "CspQuery" => new ApplicabilityRule.CspQuery(
            Required(element, "locUri"),
            (string?)element.Attribute("comparison") ?? "EqualTo", Required(element, "value")),

        "PackageVersion" => new ApplicabilityRule.PackageVersion(
            Guid.Parse(Required(element, "package")),
            (string?)element.Attribute("comparison") ?? "EqualTo", Required(element, "value")),

        "PackageInstalled" => new ApplicabilityRule.PackageInstalled(Guid.Parse(Required(element, "package"))),

        "All" => new ApplicabilityRule.All([.. element.Elements().Select(ReadRule)]),
        "Any" => new ApplicabilityRule.Any([.. element.Elements().Select(ReadRule)]),
        "Not" => new ApplicabilityRule.Negated(ReadRule(element.Elements().Single())),

        var other => throw new InvalidOperationException($"Unknown applicability rule <{other}>."),
    };

    private static string Required(XElement element, string attribute) =>
        (string?)element.Attribute(attribute)
        ?? throw new InvalidOperationException($"<{element.Name.LocalName}> is missing the '{attribute}' attribute.");
}
