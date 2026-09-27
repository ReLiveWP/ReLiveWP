using System.Xml.Linq;

namespace ReLiveWP.Dav;

public static class DavNamespaces
{
    public static readonly XNamespace WebDav = "DAV:";
    public static readonly XNamespace CardDav = "urn:ietf:params:xml:ns:carddav";
    public static readonly XNamespace CalDav = "urn:ietf:params:xml:ns:caldav";
    public static readonly XNamespace CalendarServer = "http://calendarserver.org/ns/";
}
