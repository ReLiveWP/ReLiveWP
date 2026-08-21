using System.Xml.Linq;

using static ReLiveWP.Dav.DavNamespaces;

namespace ReLiveWP.Dav;

public static class DavBody
{
    private const string Declaration = """<?xml version="1.0" encoding="utf-8"?>""";

    private static readonly Dictionary<XNamespace, string> Prefixes = new()
    {
        [WebDav] = "D",
        [CardDav] = "C",
        [CalDav] = "C",
        [CalendarServer] = "CS",
    };

    public static string Propfind(params XName[] props)
        => Serialise(new XElement(WebDav + "propfind", Prop(props)));

    public static string AddressbookQuery(params XName[] props)
        => Serialise(new XElement(CardDav + "addressbook-query", Prop(props)));

    public static string CalendarQuery(params XName[] props)
        => Serialise(new XElement(CalDav + "calendar-query", Prop(props)));

    public static string SyncCollection(string token, int level, params XName[] props)
        => Serialise(new XElement(WebDav + "sync-collection",
            new XElement(DavProps.SyncToken, token),
            new XElement(WebDav + "sync-level", level),
            Prop(props)));

    private static XElement Prop(XName[] props)
        => new(WebDav + "prop", props.Select(p => new XElement(p)));

    private static string Serialise(XElement root)
    {
        var used = root.DescendantsAndSelf()
                       .Select(e => e.Name.Namespace)
                       .Where(n => n != XNamespace.None)
                       .Distinct()
                       .ToList();

        var taken = new HashSet<string>();
        foreach (var ns in used)
        {
            var prefix = Prefixes.TryGetValue(ns, out var known) ? known : "n";

            var candidate = prefix;
            for (var i = 2; !taken.Add(candidate); i++)
                candidate = prefix + i;

            root.Add(new XAttribute(XNamespace.Xmlns + candidate, ns.NamespaceName));
        }

        return Declaration + Environment.NewLine + root;
    }
}
