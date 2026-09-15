using System.Xml.Linq;
using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

public static class ExtendedFragmentReader
{
    public static List<ParsedFragment> ReadFragments(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return [];

        var root = WsupXml.ParseFragment(xml);
        var fragments = new List<ParsedFragment>();
        var current = new List<XElement>();
        string? currentType = null;

        void Flush()
        {
            if (currentType is null || current.Count == 0)
                return;

            var body = string.Concat(current.Select(e => e.ToString(SaveOptions.DisableFormatting)));
            fragments.Add(new ParsedFragment
            {
                FragmentType = currentType,
                Language = currentType is FragmentTypes.LocalizedProperties or FragmentTypes.Eula
                    ? LanguageOf(current[0])
                    : "",
                Xml = body,
            });

            current = [];
        }

        foreach (var element in root.Elements())
        {
            var starts = StartsFragment(element.Name.LocalName);
            if (starts is not null)
            {
                Flush();
                currentType = starts;
            }

            current.Add(element);
        }

        Flush();

        for (var i = 0; i < fragments.Count; i++)
            fragments[i].Ordinal = i;

        return fragments;
    }

    private static string? StartsFragment(string elementName) => elementName switch
    {
        "UpdateIdentity" => FragmentTypes.Core,
        "ExtendedProperties" => FragmentTypes.Extended,
        "LocalizedProperties" => FragmentTypes.LocalizedProperties,
        "EulaFile" or "Eula" => FragmentTypes.Eula,
        _ => null,
    };

    private static string LanguageOf(XElement element)
    {
        var child = element.Elements().FirstOrDefault(e => e.Name.LocalName == "Language")?.Value;
        return (child ?? (string?)element.Attribute("Language") ?? "").Trim();
    }
}
