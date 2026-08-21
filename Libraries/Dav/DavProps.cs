using System.Xml.Linq;

using static ReLiveWP.Dav.DavNamespaces;

namespace ReLiveWP.Dav;

public static class DavProps
{
    public static readonly XName ResourceType = WebDav + "resourcetype";
    public static readonly XName Collection = WebDav + "collection";
    public static readonly XName DisplayName = WebDav + "displayname";
    public static readonly XName GetETag = WebDav + "getetag";
    public static readonly XName GetContentType = WebDav + "getcontenttype";
    public static readonly XName GetContentLength = WebDav + "getcontentlength";
    public static readonly XName GetLastModified = WebDav + "getlastmodified";
    public static readonly XName CurrentUserPrincipal = WebDav + "current-user-principal";
    public static readonly XName SyncToken = WebDav + "sync-token";
    public static readonly XName Href = WebDav + "href";
    public static readonly XName Status = WebDav + "status";

    public static readonly XName GetCTag = CalendarServer + "getctag";

    public static readonly XName AddressbookHomeSet = CardDav + "addressbook-home-set";
    public static readonly XName Addressbook = CardDav + "addressbook";
    public static readonly XName AddressData = CardDav + "address-data";

    public static readonly XName CalendarHomeSet = CalDav + "calendar-home-set";
    public static readonly XName Calendar = CalDav + "calendar";
    public static readonly XName CalendarData = CalDav + "calendar-data";
}
