using System.Xml.Linq;
using ReLiveWP.Dav;

namespace ReLiveWP.Dav.Tests;

// These bodies go on the wire. A missing namespace declaration or a prefix bound to the wrong URI
// fails at the server, where nothing else in the suite would catch it.
public class DavBodyTests
{
    private static XElement Root(string body) => XDocument.Parse(body).Root!;

    [Fact]
    public void EveryBodyCarriesTheXmlDeclaration()
        => Assert.StartsWith("""<?xml version="1.0" encoding="utf-8"?>""",
            DavBody.Propfind(DavProps.ResourceType));

    [Fact]
    public void PropfindAsksForTheNamedPropertiesUnderDavProp()
    {
        var root = Root(DavBody.Propfind(DavProps.ResourceType, DavProps.GetETag));

        Assert.Equal(DavNamespaces.WebDav + "propfind", root.Name);
        Assert.Equal(
            [DavProps.ResourceType, DavProps.GetETag],
            root.Element(DavNamespaces.WebDav + "prop")!.Elements().Select(e => e.Name));
    }

    [Fact]
    public void ForeignNamespacedPropertiesKeepTheirOwnNamespace()
    {
        var prop = Root(DavBody.Propfind(DavProps.SyncToken, DavProps.GetCTag))
            .Element(DavNamespaces.WebDav + "prop")!;

        Assert.Equal([DavProps.SyncToken, DavProps.GetCTag], prop.Elements().Select(e => e.Name));
    }

    [Fact]
    public void AddressbookQueryIsRootedInTheCardDavNamespace()
    {
        var root = Root(DavBody.AddressbookQuery(DavProps.GetETag, DavProps.AddressData));

        Assert.Equal(DavNamespaces.CardDav + "addressbook-query", root.Name);
        Assert.Equal(
            [DavProps.GetETag, DavProps.AddressData],
            root.Element(DavNamespaces.WebDav + "prop")!.Elements().Select(e => e.Name));
    }

    [Fact]
    public void SyncCollectionCarriesTheTokenAndLevel()
    {
        var root = Root(DavBody.SyncCollection("http://example.com/ns/sync/42", 1, DavProps.AddressData));

        Assert.Equal(DavNamespaces.WebDav + "sync-collection", root.Name);
        Assert.Equal("http://example.com/ns/sync/42", (string)root.Element(DavProps.SyncToken)!);
        Assert.Equal("1", (string)root.Element(DavNamespaces.WebDav + "sync-level")!);
    }

    // servers hand back tokens as opaque strings, and some of them are URLs with query strings
    [Fact]
    public void SyncTokensAreEscapedRatherThanBreakingTheDocument()
    {
        var token = """tok&"<>'42""";
        var root = Root(DavBody.SyncCollection(token, 1, DavProps.GetETag));

        Assert.Equal(token, (string)root.Element(DavProps.SyncToken)!);
    }

    [Fact]
    public void CalendarQueryIsRootedInTheCalDavNamespace()
        => Assert.Equal(DavNamespaces.CalDav + "calendar-query",
            Root(DavBody.CalendarQuery(DavBody.ComponentFilter("VEVENT"), DavProps.CalendarData)).Name);

    // RFC 4791 9.5 is (DAV:prop, CALDAV:filter) with no optional marker on the filter, so a query
    // without one is not a calendar-query at all and servers answer 400
    [Fact]
    public void CalendarQueryCarriesAFilterAfterTheProp()
    {
        var root = Root(DavBody.CalendarQuery(DavBody.ComponentFilter("VEVENT"), DavProps.CalendarData));

        Assert.Equal(
            [DavNamespaces.WebDav + "prop", DavNamespaces.CalDav + "filter"],
            root.Elements().Select(e => e.Name));
    }

    // RFC 4791 9.7: exactly one comp-filter, and components nest inside VCALENDAR
    [Fact]
    public void AComponentFilterNestsInsideVcalendar()
    {
        var filter = DavBody.ComponentFilter("VTODO");
        var outer = Assert.Single(filter.Elements(DavNamespaces.CalDav + "comp-filter"));

        Assert.Equal("VCALENDAR", (string)outer.Attribute("name")!);
        Assert.Equal("VTODO",
            (string)Assert.Single(outer.Elements(DavNamespaces.CalDav + "comp-filter")).Attribute("name")!);
    }

    // RFC 4791 9.10: (DAV:prop, DAV:href+)
    [Fact]
    public void CalendarMultigetListsEveryHrefAfterTheProp()
    {
        var root = Root(DavBody.CalendarMultiget(
            ["/cal/1.ics", "/cal/2.ics"], DavProps.GetETag, DavProps.CalendarData));

        Assert.Equal(DavNamespaces.CalDav + "calendar-multiget", root.Name);
        Assert.Equal(DavNamespaces.WebDav + "prop", root.Elements().First().Name);
        Assert.Equal(
            ["/cal/1.ics", "/cal/2.ics"],
            root.Elements(DavProps.Href).Select(e => (string)e));
    }
}
