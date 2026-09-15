using System.Xml.Linq;
using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

public static class SyncUpdatesParser
{
    public static List<ParsedUpdate> ParseResponse(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        XNamespace ns = WsupXml.ServiceNs;

        var results = new List<ParsedUpdate>();
        foreach (var info in doc.Descendants(ns + "UpdateInfo"))
            results.Add(ParseUpdateInfo(info, ns));

        return results;
    }

    private static ParsedUpdate ParseUpdateInfo(XElement info, XNamespace ns)
    {
        var update = new ParsedUpdate
        {
            RevisionId = (long)info.Element(ns + "ID")!,
            IsLeaf = (bool?)info.Element(ns + "IsLeaf") ?? false,
        };

        var deployment = info.Element(ns + "Deployment");
        if (deployment is not null)
        {
            update.DeploymentAction = (string?)deployment.Element(ns + "Action") ?? "Evaluate";
            update.LastChangeTime = WsupXml.AsUtc((DateTime?)deployment.Element(ns + "LastChangeTime"));
        }
        update.IsBundle = update.DeploymentAction == "Bundle";

        var inner = info.Element(ns + "Xml")?.Value ?? "";
        update.SyncMetadataXml = inner;
        ParseMetadata(inner, update);

        return update;
    }

    public static ParsedUpdate ParseMetadataOnly(string innerXml)
    {
        var update = new ParsedUpdate { SyncMetadataXml = innerXml };
        ParseMetadata(innerXml, update);
        return update;
    }

    private static void ParseMetadata(string innerXml, ParsedUpdate update)
    {
        if (string.IsNullOrWhiteSpace(innerXml))
            return;

        var root = WsupXml.ParseFragment(innerXml);

        var identity = root.Elements().FirstOrDefault(e => e.Name.LocalName == "UpdateIdentity");
        if (identity is not null)
        {
            update.UpdateId = Guid.Parse((string)identity.Attribute("UpdateID")!);
            update.RevisionNumber = (int?)identity.Attribute("RevisionNumber") ?? 0;
        }

        var props = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Properties");
        var type = (string?)props?.Attribute("UpdateType");
        update.UpdateType = type switch
        {
            "Category" => UpdateType.Category,
            "Detectoid" => UpdateType.Detectoid,
            _ => UpdateType.Software,
        };

        var relationships = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "Relationships");
        if (relationships is null)
            return;

        var prereqs = relationships.Descendants().FirstOrDefault(e => e.Name.LocalName == "Prerequisites");
        if (prereqs is not null)
        {
            var group = 0;
            foreach (var child in prereqs.Elements())
            {
                if (child.Name.LocalName == "UpdateIdentity")
                {
                    update.Prerequisites.Add(new ParsedPrerequisite
                    {
                        PrerequisiteUpdateId = Guid.Parse((string)child.Attribute("UpdateID")!),
                        GroupId = 0,
                    });
                }
                else if (child.Name.LocalName == "AtLeastOne")
                {
                    group++;
                    var isCategory = (bool?)child.Attribute("IsCategory") ?? false;
                    foreach (var member in child.Elements().Where(e => e.Name.LocalName == "UpdateIdentity"))
                    {
                        update.Prerequisites.Add(new ParsedPrerequisite
                        {
                            PrerequisiteUpdateId = Guid.Parse((string)member.Attribute("UpdateID")!),
                            GroupId = group,
                            IsCategory = isCategory,
                        });
                    }
                }
            }
        }

        var bundled = relationships.Descendants().FirstOrDefault(e => e.Name.LocalName == "BundledUpdates");
        if (bundled is not null)
        {
            foreach (var member in bundled.Descendants().Where(e => e.Name.LocalName == "UpdateIdentity"))
            {
                update.Bundles.Add(new ParsedBundle
                {
                    BundledUpdateId = Guid.Parse((string)member.Attribute("UpdateID")!),
                    BundledRevisionNumber = (int?)member.Attribute("RevisionNumber"),
                });
            }
        }
    }
}
