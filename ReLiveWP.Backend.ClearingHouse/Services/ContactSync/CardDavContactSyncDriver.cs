using System.Runtime.CompilerServices;
using Google.Protobuf.WellKnownTypes;
using ReLiveWP.Dav;
using ReLiveWP.Services.Grpc.Mailbox;
using FolkerKinzel.VCards;
using FolkerKinzel.VCards.Enums;
using PCl = FolkerKinzel.VCards.Enums.PCl;
using FolkerKinzel.VCards.Models.Properties;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public class CardDavContactSyncDriver(
    ConnectedServicesProxy proxy,
    ILogger<CardDavContactSyncDriver> logger) : IContactSyncDriver
{
    public const string ServiceName = "carddav";

    private static readonly string SyncTokenBody = DavBody.Propfind(DavProps.SyncToken, DavProps.GetCTag);

    private static readonly string ListBody = DavBody.Propfind(DavProps.ResourceType, DavProps.DisplayName);

    private static readonly string QueryBody = DavBody.AddressbookQuery(DavProps.GetETag, DavProps.AddressData);

    public string ServiceId => ServiceName;

    public async Task<IReadOnlyList<RemoteContactSource>> ListSourcesAsync(
        SyncConnection connection, CancellationToken ct = default)
    {
        var multistatus = await SendAsync(connection, DavMethods.Propfind, "", ListBody, depth: "1", ct);
        var sources = new List<RemoteContactSource>();

        foreach (var response in multistatus.Responses)
        {
            if (!response.IsResourceType(DavProps.Addressbook)) continue;

            var relative = DavPath.StripPrefix(response.Href, connection.ServiceUrl);
            var name = response.DisplayName;

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

        var multistatus = await SendAsync(connection, DavMethods.Report, sourceId, QueryBody, depth: "1", ct);
        var (contacts, unreadable) = ReadContacts(multistatus, connection);
        var token = await ReadSyncTokenAsync(connection, sourceId, ct);

        return new ContactSyncBatch(contacts, [], token, IsFullSync: true, unreadable);
    }

    private async Task<ContactSyncBatch> FetchDeltaAsync(
        SyncConnection connection, string sourceId, string deltaToken, CancellationToken ct)
    {
        var body = DavBody.SyncCollection(deltaToken, 1, DavProps.GetETag, DavProps.AddressData);

        var multistatus = await SendAsync(connection, DavMethods.Report, sourceId, body, depth: "1", ct);

        var (contacts, unreadable) = ReadContacts(multistatus, connection);

        var deleted = multistatus.NotFound
            .Select(r => DavPath.StripPrefix(r.Href, connection.ServiceUrl))
            .ToList();

        return new ContactSyncBatch(contacts, deleted, multistatus.SyncToken, IsFullSync: false);
    }

    private (List<RemoteContact> Contacts, List<string> Unreadable) ReadContacts(
        DavMultiStatus multistatus, SyncConnection connection)
    {
        var contacts = new List<RemoteContact>();
        var unreadable = new List<string>();

        foreach (var response in multistatus.Found)
        {
            var externalId = DavPath.StripPrefix(response.Href, connection.ServiceUrl);

            if (externalId.Length == 0 || externalId.EndsWith('/')) continue;

            var data = response.Value(DavProps.AddressData);
            if (string.IsNullOrWhiteSpace(data))
            {
                logger.LogWarning("no address-data returned for {Href}", response.Href);
                unreadable.Add(externalId);
                continue;
            }

            try
            {
                if (Project(externalId, response.ETag, data) is { } contact)
                    contacts.Add(contact);
                else
                    unreadable.Add(externalId);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "could not parse the vCard at {Href}", response.Href);
                unreadable.Add(externalId);
            }
        }

        return (contacts, unreadable);
    }

    private async Task<string?> ReadSyncTokenAsync(SyncConnection connection, string sourceId, CancellationToken ct)
    {
        try
        {
            var multistatus = await SendAsync(connection, DavMethods.Propfind, sourceId, SyncTokenBody, depth: "0", ct);

            return multistatus.SyncToken
                ?? multistatus.Responses.Select(r => r.Value(DavProps.SyncToken)).FirstOrDefault(v => v is not null)
                ?? multistatus.Responses.Select(r => r.Value(DavProps.GetCTag)).FirstOrDefault(v => v is not null);
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

    private async Task<DavMultiStatus> SendAsync(
        SyncConnection connection, HttpMethod method, string path, string body, string depth, CancellationToken ct)
    {
        using var dav = proxy.CreateDavClient(ServiceName, connection);

        try
        {
            return method == DavMethods.Report
                ? await dav.ReportAsync(path, body, depth, ct)
                : await dav.PropfindAsync(path, body, depth, ct);
        }
        catch (DavSyncTokenException e)
        {
            throw new DeltaTokenExpiredException(e.Message);
        }
        catch (DavParseException e)
        {
            throw new ContactSyncException($"CardDAV {method} {path}: {e.Message}");
        }
        catch (DavException e)
        {
            throw new ContactSyncException($"CardDAV {method} {path} failed: {e.Message}");
        }
    }
}
