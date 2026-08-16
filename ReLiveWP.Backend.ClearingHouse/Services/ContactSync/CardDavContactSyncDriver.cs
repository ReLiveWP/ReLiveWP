using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Google.Protobuf.WellKnownTypes;
using ReLiveWP.Services.Grpc.Mailbox;
using FolkerKinzel.VCards;
using FolkerKinzel.VCards.Enums;
using PCl = FolkerKinzel.VCards.Enums.PCl;
using FolkerKinzel.VCards.Models.Properties;
using System.Security;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public class CardDavContactSyncDriver(
    ConnectedServicesProxy proxy,
    ILogger<CardDavContactSyncDriver> logger) : IContactSyncDriver
{
    public const string ServiceName = "carddav";

    private static readonly HttpMethod Propfind = new("PROPFIND");
    private static readonly HttpMethod Report = new("REPORT");

    private static readonly XNamespace Dav = "DAV:";
    private static readonly XNamespace Card = "urn:ietf:params:xml:ns:carddav";
    private static readonly XNamespace CalendarServer = "http://calendarserver.org/ns/";

    private const string SyncTokenBody =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <D:propfind xmlns:D="DAV:" xmlns:CS="http://calendarserver.org/ns/"><D:prop>
          <D:sync-token/><CS:getctag/>
        </D:prop></D:propfind>
        """;

    private const string ListBody =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <D:propfind xmlns:D="DAV:"><D:prop>
          <D:resourcetype/><D:displayname/>
        </D:prop></D:propfind>
        """;

    private const string QueryBody =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <C:addressbook-query xmlns:D="DAV:" xmlns:C="urn:ietf:params:xml:ns:carddav">
          <D:prop><D:getetag/><C:address-data/></D:prop>
        </C:addressbook-query>
        """;

    public string ServiceId => ServiceName;

    public async Task<IReadOnlyList<RemoteContactSource>> ListSourcesAsync(
        SyncConnection connection, CancellationToken ct = default)
    {
        var multistatus = await SendAsync(connection, Propfind, "", ListBody, depth: "1", ct);
        var sources = new List<RemoteContactSource>();

        foreach (var response in multistatus.Elements(Dav + "response"))
        {
            var href = (string?)response.Element(Dav + "href");
            if (href is null) continue;

            var isAddressBook = response
                .Descendants(Dav + "resourcetype")
                .Any(rt => rt.Element(Card + "addressbook") is not null);

            if (!isAddressBook) continue;

            var relative = Relative(href, connection.ServiceUrl);
            var name = response.Descendants(Dav + "displayname").Select(x => (string?)x).FirstOrDefault();

            sources.Add(new RemoteContactSource(
                relative,
                string.IsNullOrWhiteSpace(name) ? relative : name));
        }

        return sources;
    }

    public async Task<ContactSyncBatch> FetchChangesAsync(
        SyncConnection connection, string sourceId, string? deltaToken, CancellationToken ct = default)
    {
        if (deltaToken is not null)
        {
            try
            {
                return await FetchDeltaAsync(connection, sourceId, deltaToken, ct);
            }
            catch (DeltaTokenExpiredException e)
            {
                logger.LogInformation("CardDAV sync token for {Source} is stale ({Reason}), falling back to a full pull",
                    sourceId, e.Message);
            }
        }

        var multistatus = await SendAsync(connection, Report, sourceId, QueryBody, depth: "1", ct);
        var (contacts, unreadable) = ReadContacts(multistatus, connection);
        var token = await ReadSyncTokenAsync(connection, sourceId, ct);

        return new ContactSyncBatch(contacts, [], token, IsFullSync: true, unreadable);
    }

    private async Task<ContactSyncBatch> FetchDeltaAsync(
        SyncConnection connection, string sourceId, string deltaToken, CancellationToken ct)
    {
        var body = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <D:sync-collection xmlns:D="DAV:" xmlns:C="urn:ietf:params:xml:ns:carddav">
              <D:sync-token>{SecurityElement.Escape(deltaToken)}</D:sync-token>
              <D:sync-level>1</D:sync-level>
              <D:prop><D:getetag/><C:address-data/></D:prop>
            </D:sync-collection>
            """;

        var multistatus = await SendAsync(connection, Report, sourceId, body, depth: "1", ct);

        var (contacts, unreadable) = ReadContacts(multistatus, connection);
        var deleted = new List<string>();

        foreach (var response in multistatus.Elements(Dav + "response"))
        {
            var status = (string?)response.Element(Dav + "status");
            if (status is null || !status.Contains("404")) continue;

            if ((string?)response.Element(Dav + "href") is { } href)
                deleted.Add(Relative(href, connection.ServiceUrl));
        }

        var token = (string?)multistatus.Element(Dav + "sync-token");

        return new ContactSyncBatch(contacts, deleted, token, IsFullSync: false);
    }

    private (List<RemoteContact> Contacts, List<string> Unreadable) ReadContacts(
        XElement multistatus, SyncConnection connection)
    {
        var contacts = new List<RemoteContact>();
        var unreadable = new List<string>();

        foreach (var response in multistatus.Elements(Dav + "response"))
        {
            var href = (string?)response.Element(Dav + "href");
            if (href is null) continue;

            var status = (string?)response.Element(Dav + "status");
            if (status is not null && status.Contains("404")) continue;

            var externalId = Relative(href, connection.ServiceUrl);

            if (externalId.Length == 0 || externalId.EndsWith('/')) continue;

            var data = response.Descendants(Card + "address-data").Select(x => (string?)x).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(data))
            {
                logger.LogWarning("no address-data returned for {Href}", href);
                unreadable.Add(externalId);
                continue;
            }

            var etag = response.Descendants(Dav + "getetag").Select(x => (string?)x).FirstOrDefault();

            try
            {
                if (Project(externalId, etag, data) is { } contact)
                    contacts.Add(contact);
                else
                    unreadable.Add(externalId);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "could not parse the vCard at {Href}", href);
                unreadable.Add(externalId);
            }
        }

        return (contacts, unreadable);
    }

    private async Task<string?> ReadSyncTokenAsync(SyncConnection connection, string sourceId, CancellationToken ct)
    {
        try
        {
            var multistatus = await SendAsync(connection, Propfind, sourceId, SyncTokenBody, depth: "0", ct);

            return multistatus.Descendants(Dav + "sync-token").Select(x => (string?)x).FirstOrDefault()
                ?? multistatus.Descendants(CalendarServer + "getctag").Select(x => (string?)x).FirstOrDefault();
        }
        catch (ContactSyncException e)
        {
            logger.LogInformation("CardDAV collection {Source} reports no sync token ({Reason}); every poll will be a full pull",
                sourceId, e.Message);
            return null;
        }
    }

    internal static RemoteContact? Project(string externalId, string? etag, string vcard)
    {
        var card = Vcf.Parse(vcard).FirstOrDefault();
        if (card is null) return null;

        var item = new ContactItem();

        if (card.NameViews?.FirstOrDefault()?.Value is { } name)
        {
            ContactSlots.Set(name.Given.FirstOrDefault(), v => item.FirstName = v);
            ContactSlots.Set(name.Given2.FirstOrDefault(), v => item.MiddleName = v);
            ContactSlots.Set(name.Surnames.FirstOrDefault(), v => item.LastName = v);
            ContactSlots.Set(name.Prefixes.FirstOrDefault(), v => item.Title = v);
            ContactSlots.Set(name.Suffixes.FirstOrDefault(), v => item.Suffix = v);
        }

        ContactSlots.Set(card.NickNames?.FirstOrDefault()?.Value?.FirstOrDefault(), v => item.NickName = v);
        ContactSlots.Set(card.Notes?.FirstOrDefault()?.Value, v => item.Notes = v);
        ContactSlots.Set(card.Titles?.FirstOrDefault()?.Value, v => item.JobTitle = v);
        ContactSlots.Set(card.Urls?.FirstOrDefault()?.Value, v => item.WebPage = v);

        if (card.Organizations?.FirstOrDefault()?.Value is { } org)
        {
            ContactSlots.Set(org.Name, v => item.CompanyName = v);
            ContactSlots.Set(org.Units?.FirstOrDefault(), v => item.Department = v);
        }

        ContactSlots.SetEmails(item, card.EMails?.Select(e => e?.Value).OfType<string>() ?? []);
        ContactSlots.SetPhones(item, Phones(card));
        ApplyAddresses(item, card);

        if (card.BirthDayViews?.FirstOrDefault()?.Value is { } bday && bday.TryAsDateOnly(out var born))
            item.Birthday = Timestamp.FromDateTime(ContactDates.Clamp(
                new DateTime(born.Year, born.Month, born.Day, 0, 0, 0, DateTimeKind.Utc)));

        foreach (var category in card.Categories?.FirstOrDefault()?.Value ?? [])
            if (!string.IsNullOrWhiteSpace(category))
                item.Categories.Add(new ContactCategory { Name = category });

        ContactSlots.Set(card.DisplayNames?.FirstOrDefault()?.Value, v => item.FileAs = v);
        if (!item.HasFileAs)
            ContactSlots.Set(ContactSlots.BuildFileAs(item), v => item.FileAs = v);

        var photo = card.Photos?.FirstOrDefault();
        var data = photo?.Value;

        return new RemoteContact(externalId, item, etag,
            data?.Uri?.ToString(), data?.Bytes,
            data?.Uri is not null ? ServiceName : null,
            photo is null ? null : ReadCrop(photo));
    }

    internal static PhotoCrop? ReadCrop(VCardProperty photo)
    {
        var raw = photo.Parameters.NonStandard?
            .FirstOrDefault(p => p.Key.Equals("X-ABCROP-RECTANGLE", StringComparison.OrdinalIgnoreCase))
            .Value;

        if (raw is null) return null;

        var parts = raw.Split('&');

        return parts.Length >= 5
            && int.TryParse(parts[1], out var x)
            && int.TryParse(parts[2], out var y)
            && int.TryParse(parts[3], out var width)
            && int.TryParse(parts[4], out var height)
            && width > 0 && height > 0
                ? new PhotoCrop(x, y, width, height, OriginIsBottomLeft: true)
                : null;
    }

    private static IEnumerable<(PhoneSlot, string)> Phones(VCard card)
    {
        foreach (var phone in card.Phones ?? [])
        {
            if (phone?.Value is not { Length: > 0 } number) continue;

            var kind = phone.Parameters.PhoneType;
            var where = phone.Parameters.PropertyClass;

            var home = where?.HasFlag(PCl.Home) == true;
            var work = where?.HasFlag(PCl.Work) == true;

            yield return (kind switch
            {
                _ when kind?.HasFlag(Tel.Cell) == true => PhoneSlot.Mobile,
                _ when kind?.HasFlag(Tel.Fax) == true && home => PhoneSlot.HomeFax,
                _ when kind?.HasFlag(Tel.Fax) == true => PhoneSlot.BusinessFax,
                _ when kind?.HasFlag(Tel.Pager) == true => PhoneSlot.Pager,
                _ when kind?.HasFlag(Tel.Car) == true => PhoneSlot.Car,
                _ when work => PhoneSlot.Business,
                _ when home => PhoneSlot.Home,
                _ => PhoneSlot.Other,
            }, number);
        }
    }

    private static void ApplyAddresses(ContactItem item, VCard card)
    {
        var taken = new HashSet<AddressSlot>();

        foreach (var address in card.Addresses ?? [])
        {
            if (address?.Value is not { } value) continue;

            var where = address.Parameters.PropertyClass;
            var slot = where?.HasFlag(PCl.Home) == true ? AddressSlot.Home
                     : where?.HasFlag(PCl.Work) == true ? AddressSlot.Business
                     : AddressSlot.Other;

            if (!taken.Add(slot)) continue;

            ContactSlots.SetAddress(item, slot,
                value.Street.FirstOrDefault(), value.Locality.FirstOrDefault(),
                value.Region.FirstOrDefault(), value.PostalCode.FirstOrDefault(),
                value.Country.FirstOrDefault());
        }
    }

    private async Task<XElement> SendAsync(
        SyncConnection connection, HttpMethod method, string path, string body, string depth, CancellationToken ct)
    {
        using var request = proxy.Request(method, ServiceName, path, connection);
        request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/xml");
        request.Headers.TryAddWithoutValidation("Depth", depth);

        using var response = await proxy.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
        var xml = await response.Content.ReadAsStringAsync(ct);

        if ((int)response.StatusCode == 403 && xml.Contains("valid-sync-token", StringComparison.OrdinalIgnoreCase))
            throw new DeltaTokenExpiredException($"the server rejected the sync token for {path}");

        if (!response.IsSuccessStatusCode && (int)response.StatusCode != 207)
            throw new ContactSyncException(
                $"CardDAV {method} {path} failed ({(int)response.StatusCode}): {ConnectedServicesProxy.Truncate(xml)}");

        return ParseMultistatus(xml);
    }

    internal static XElement ParseMultistatus(string xml)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Parse(xml);
        }
        catch (Exception e)
        {
            throw new ContactSyncException($"CardDAV returned unparseable XML: {e.Message}");
        }

        return doc.Root?.Name == Dav + "multistatus"
            ? doc.Root
            : throw new ContactSyncException($"CardDAV returned {doc.Root?.Name.LocalName ?? "nothing"}, expected multistatus");
    }
    internal static string Relative(string href, string? serviceUrl)
    {
        var path = Uri.TryCreate(href, UriKind.Absolute, out var absolute) ? absolute.AbsolutePath : href;

        if (!string.IsNullOrEmpty(serviceUrl) &&
            Uri.TryCreate(serviceUrl, UriKind.Absolute, out var root))
        {
            var prefix = root.AbsolutePath.TrimEnd('/');
            if (prefix.Length > 0 && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                path = path[prefix.Length..];
        }

        return path.TrimStart('/');
    }

}
