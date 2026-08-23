using System.Runtime.CompilerServices;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using ReLiveWP.Backend.ClearingHouse.Services.Mirror;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public class GoogleContactSyncDriver(
    ConnectedServicesProxy proxy,
    ILogger<GoogleContactSyncDriver> logger) : IMirrorDriver
{
    public const string ServiceName = "google";
    public const string DefaultSourceId = "people/me/connections";

    private const string Host = "people.googleapis.com";
    private const int PageSize = 200;
    private const int MaxPages = 200;

    private const string PersonFields =
        "names,nicknames,emailAddresses,phoneNumbers,addresses,organizations,biographies,urls," +
        "birthdays,relations,imClients,photos,metadata";

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public string ServiceId => ServiceName;

    public MirrorKind Kind => MirrorKind.Contacts;

    public Task<IReadOnlyList<RemoteSource>> ListSourcesAsync(
        SyncConnection connection, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RemoteSource>>(
            [new RemoteSource(DefaultSourceId, "Google Contacts", IsDefault: true)]);

    public async Task<MirrorBatch> FetchChangesAsync(
        SyncConnection connection, string sourceId, string? deltaToken, CancellationToken ct = default)
    {
        var contacts = new List<RemoteContact>();
        var deleted = new List<string>();

        string? pageToken = null;
        string? nextSyncToken = null;
        var pages = 0;

        do
        {
            var path = $"{Host}/v1/people/me/connections" +
                       $"?personFields={Uri.EscapeDataString(PersonFields)}&pageSize={PageSize}" +
                       "&requestSyncToken=true";

            if (deltaToken is not null)
                path += $"&syncToken={Uri.EscapeDataString(deltaToken)}";
            if (pageToken is not null)
                path += $"&pageToken={Uri.EscapeDataString(pageToken)}";

            using var request = proxy.Request(HttpMethod.Get, ServiceName, path, connection);
            using var response = await proxy.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            if ((int)response.StatusCode == 410 && deltaToken is not null)
                throw new DeltaTokenExpiredException("Google rejected the sync token; a full sync is required.");

            if (!response.IsSuccessStatusCode)
                throw new MirrorException(
                    $"Google People connections.list failed ({(int)response.StatusCode}): {ConnectedServicesProxy.Truncate(json)}");

            GooglePeopleResponse? page;
            try
            {
                page = JsonSerializer.Deserialize<GooglePeopleResponse>(json, Json);
            }
            catch (JsonException e)
            {
                throw new MirrorException($"Google People returned unreadable JSON: {e.Message}");
            }

            foreach (var element in page?.Connections ?? [])
            {
                if (IsDeleted(element))
                {
                    if (ResourceName(element) is { } gone) deleted.Add(gone);
                    continue;
                }

                if (Project(element) is { } contact) contacts.Add(contact);
            }

            pageToken = page?.NextPageToken;
            nextSyncToken = page?.NextSyncToken ?? nextSyncToken;
            pages++;

            if (pages >= MaxPages && pageToken is not null)
            {
                logger.LogWarning("Google contact fetch for {User} hit {Pages} pages with more available", connection.UserId, pages);
                throw new MirrorException($"Google returned more than {MaxPages} pages of contacts; refusing to truncate.");
            }
        }
        while (pageToken is not null);

        return new MirrorBatch(contacts, deleted, nextSyncToken, IsFullSync: deltaToken is null);
    }

    private static bool IsDeleted(JsonElement element) =>
        element.TryGetProperty("metadata", out var metadata) &&
        metadata.TryGetProperty("deleted", out var deleted) &&
        deleted.ValueKind == JsonValueKind.True;

    private static string? ResourceName(JsonElement element) =>
        element.TryGetProperty("resourceName", out var name) ? name.GetString() : null;

    internal static RemoteContact? Project(JsonElement element)
    {
        var person = element.Deserialize<GooglePerson>(Json);
        if (person?.ResourceName is not { Length: > 0 } resourceName) return null;

        var item = new ContactItem();
        var name = person.Names.FirstOrDefault();
        var org = person.Organizations.FirstOrDefault();

        if (name is not null)
        {
            ContactSlots.Set(name.GivenName, v => item.FirstName = v);
            ContactSlots.Set(name.MiddleName, v => item.MiddleName = v);
            ContactSlots.Set(name.FamilyName, v => item.LastName = v);
            ContactSlots.Set(name.HonorificPrefix, v => item.Title = v);
            ContactSlots.Set(name.HonorificSuffix, v => item.Suffix = v);
            ContactSlots.Set(name.DisplayName, v => item.FileAs = v);
        }

        ContactSlots.Set(person.Nicknames.FirstOrDefault()?.Value, v => item.NickName = v);

        if (org is not null)
        {
            ContactSlots.Set(org.Name, v => item.CompanyName = v);
            ContactSlots.Set(org.Title, v => item.JobTitle = v);
            ContactSlots.Set(org.Department, v => item.Department = v);
            ContactSlots.Set(org.Location, v => item.OfficeLocation = v);
        }

        ContactSlots.SetEmails(item, person.EmailAddresses.Select(e => e.Value).OfType<string>());
        ContactSlots.SetImAddresses(item, person.ImClients.Select(i => i.Username).OfType<string>());
        ContactSlots.SetPhones(item, Phones(person));
        ApplyAddresses(item, person);

        ContactSlots.Set(person.Biographies.FirstOrDefault()?.Value, v => item.Notes = v);
        ContactSlots.Set(person.Urls.FirstOrDefault()?.Value, v => item.WebPage = v);
        ContactSlots.Set(Spouse(person), v => item.Spouse = v);

        if (!item.HasFileAs)
            ContactSlots.Set(ContactSlots.BuildFileAs(item), v => item.FileAs = v);

        if (Birthday(person) is { } birthday)
            item.Birthday = Timestamp.FromDateTime(birthday);

        return new RemoteContact(
            resourceName,
            item,
            person.Etag,
            PhotoUrl(person));
    }

    private static IEnumerable<(PhoneSlot, string)> Phones(GooglePerson person)
    {
        foreach (var phone in person.PhoneNumbers)
        {
            if (phone.Value is not { Length: > 0 } value) continue;

            yield return (phone.Type switch
            {
                "mobile" => PhoneSlot.Mobile,
                "home" => PhoneSlot.Home,
                "work" => PhoneSlot.Business,
                "workMobile" => PhoneSlot.Business2,
                "homeFax" => PhoneSlot.HomeFax,
                "workFax" or "otherFax" => PhoneSlot.BusinessFax,
                "pager" => PhoneSlot.Pager,
                "car" => PhoneSlot.Car,
                "main" => PhoneSlot.CompanyMain,
                "radio" => PhoneSlot.Radio,
                "assistant" => PhoneSlot.Assistant,
                _ => PhoneSlot.Other,
            }, value);
        }
    }

    private static void ApplyAddresses(ContactItem item, GooglePerson person)
    {
        var taken = new HashSet<AddressSlot>();

        foreach (var address in person.Addresses)
        {
            var slot = address.Type switch
            {
                "home" => AddressSlot.Home,
                "work" => AddressSlot.Business,
                _ => AddressSlot.Other,
            };

            if (!taken.Add(slot)) continue;

            ContactSlots.SetAddress(item, slot,
                address.StreetAddress, address.City, address.Region,
                address.PostalCode, address.Country);
        }
    }

    private static DateTime? Birthday(GooglePerson person)
    {
        if (person.Birthdays.FirstOrDefault()?.Date is not { } date) return null;
        if (date.Month is not { } month || date.Day is not { } day) return null;

        try
        {
            return ContactDates.Clamp(
                new DateTime(date.Year ?? ContactDates.UnknownYear, month, day, 0, 0, 0, DateTimeKind.Utc));
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static string? Spouse(GooglePerson person) => person.Relations
        .FirstOrDefault(r => string.Equals(r.Type, "spouse", StringComparison.OrdinalIgnoreCase))?.Person;

    private static string? PhotoUrl(GooglePerson person)
    {
        if (person.Photos.FirstOrDefault(p => !p.Default && p.Url is { Length: > 0 })?.Url is not { } url)
            return null;

        var suffix = url.LastIndexOf('=');
        if (suffix > 0) url = url[..suffix];

        return $"{url}=s{ContactPhotoService.TileSize}-c";
    }

}
