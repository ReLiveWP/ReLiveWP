using System.Xml.Linq;
using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Catalog;

// Builds the SyncUpdates "Core" metadata fragment (MS-WUSP 3.1.1.1). The scaffolding is
// namespace-less and the rule elements each carry their own xmlns, which is not tidy but is exactly
// what upstream emits and what the WP7 parser has been proven against.
public static class CoreFragmentWriter
{
    private static readonly XNamespace Base = "http://schemas.microsoft.com/msus/2002/12/BaseApplicabilityRules";
    private static readonly XNamespace Mobile = "http://schemas.microsoft.com/msus/2002/12/MobileApplicabilityRules";
    private static readonly XNamespace Logical = "http://schemas.microsoft.com/msus/2002/12/LogicalApplicabilityRules";

    private const string LocalMachine = "HKEY_LOCAL_MACHINE";

    public static string Write(ManifestUpdate update)
    {
        var parts = new List<XElement>
        {
            new("UpdateIdentity",
                new XAttribute("UpdateID", Format(update.UpdateId)),
                new XAttribute("RevisionNumber", update.RevisionNumber)),
            WriteProperties(update),
        };

        var relationships = WriteRelationships(update);
        if (relationships is not null)
            parts.Add(relationships);

        var applicability = WriteApplicabilityRules(update);
        if (applicability is not null)
            parts.Add(applicability);

        return string.Concat(parts.Select(p => p.ToString(SaveOptions.DisableFormatting)));
    }

    private static XElement WriteProperties(ManifestUpdate update)
    {
        var properties = new XElement("Properties", new XAttribute("UpdateType", update.UpdateType.ToString()));

        if (update.ExplicitlyDeployable)
            properties.Add(new XAttribute("ExplicitlyDeployable", "true"));

        return properties;
    }

    private static XElement? WriteRelationships(ManifestUpdate update)
    {
        var hasPrerequisites = update.RequiredUpdateIds.Count > 0 || update.Choices.Count > 0;
        if (!hasPrerequisites && update.BundledUpdateIds.Count == 0)
            return null;

        var relationships = new XElement("Relationships");

        if (hasPrerequisites)
        {
            var prerequisites = new XElement("Prerequisites");

            foreach (var id in update.RequiredUpdateIds)
                prerequisites.Add(Identity(id));

            foreach (var choice in update.Choices)
            {
                var atLeastOne = new XElement("AtLeastOne", choice.UpdateIds.Select(Identity));
                if (choice.IsCategory)
                    atLeastOne.SetAttributeValue("IsCategory", "true");

                prerequisites.Add(atLeastOne);
            }

            relationships.Add(prerequisites);
        }

        if (update.BundledUpdateIds.Count > 0)
            relationships.Add(new XElement("BundledUpdates", update.BundledUpdateIds.Select(Identity)));

        return relationships;
    }

    private static XElement? WriteApplicabilityRules(ManifestUpdate update)
    {
        if (update.IsInstalled is null && update.IsSuperseded is null && update.IsInstallable is null)
            return null;

        var rules = new XElement("ApplicabilityRules");

        if (update.IsInstalled is not null)
            rules.Add(new XElement("IsInstalled", WriteRule(update.IsInstalled)));

        if (update.IsSuperseded is not null)
            rules.Add(new XElement("IsSuperseded", WriteRule(update.IsSuperseded)));

        if (update.IsInstallable is not null)
            rules.Add(new XElement("IsInstallable", WriteRule(update.IsInstallable)));

        return rules;
    }

    private static XElement WriteRule(ApplicabilityRule rule)
    {
        DeviceRuleSupport.Validate(rule);
        return WriteValidatedRule(rule);
    }

    private static XElement WriteValidatedRule(ApplicabilityRule rule) => rule switch
    {
        ApplicabilityRule.RegSz r => new XElement(Base + "b.RegSz",
            new XAttribute("Key", LocalMachine),
            new XAttribute("Subkey", r.Subkey),
            new XAttribute("Value", r.Value),
            new XAttribute("Comparison", r.Comparison),
            new XAttribute("Data", r.Data)),

        ApplicabilityRule.RegDword r => new XElement(Base + "b.RegDword",
            new XAttribute("Key", LocalMachine),
            new XAttribute("Subkey", r.Subkey),
            new XAttribute("Value", r.Value),
            new XAttribute("Comparison", r.Comparison),
            new XAttribute("Data", r.Data)),

        ApplicabilityRule.CspQuery r => CspQuery(r.LocUri, r.Comparison, r.Value),

        ApplicabilityRule.PackageVersion r =>
            CspQuery(DeviceRuleSupport.PkgVersionUri(r.Package), r.Comparison, r.Value),

        ApplicabilityRule.PackageInstalled r =>
            CspQuery(DeviceRuleSupport.PackageStateUri(r.Package), "EqualTo", DeviceRuleSupport.InstalledPackageState),

        ApplicabilityRule.All r => new XElement(Logical + "And", r.Rules.Select(WriteValidatedRule)),

        ApplicabilityRule.Any r => new XElement(Logical + "Or", r.Rules.Select(WriteValidatedRule)),

        ApplicabilityRule.Negated r => new XElement(Logical + "Not", WriteValidatedRule(r.Rule)),

        _ => throw new NotSupportedException($"No writer for applicability rule {rule.GetType().Name}"),
    };

    private static XElement CspQuery(string locUri, string comparison, string value) =>
        new(Mobile + "CspQuery",
            new XAttribute("LocUri", locUri),
            new XAttribute("Comparison", comparison),
            new XAttribute("Value", value));

    private static XElement Identity(Guid updateId) =>
        new("UpdateIdentity", new XAttribute("UpdateID", Format(updateId)));

    private static string Format(Guid updateId) => updateId.ToString("D");
}
