using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public class GraphContactSyncDriver(
    ConnectedServicesProxy proxy,
    ILogger<GraphContactSyncDriver> logger) : IContactSyncDriver
{
    public const string ServiceName = "microsoft";

    // the default contacts folder is not one of the contactFolders Graph lists, so it gets a name here
    public const string DefaultSourceId = "contacts";

    private const string Host = "graph.microsoft.com";
    private const int PageSize = 100;
    private const int MaxPages = 200;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public string ServiceId => ServiceName;

    public async Task<IReadOnlyList<RemoteContactSource>> ListSourcesAsync(
        SyncConnection connection, CancellationToken ct = default)
    {
        var sources = new List<RemoteContactSource>
        {
            new(DefaultSourceId, "Contacts", IsDefault: true),
        };

        var path = $"{Host}/v1.0/me/contactFolders?$select=id,displayName&$top={PageSize}";

        for (var page = 0; page < MaxPages && path is not null; page++)
        {
            var json = await SendAsync(connection, path, ct);
            var response = Deserialize<GraphFoldersResponse>(json);

            foreach (var folder in response?.Value ?? [])
            {
                if (folder.Id is { Length: > 0 } id)
                    sources.Add(new RemoteContactSource(id, folder.DisplayName ?? id));
            }

            path = response?.NextLink is { Length: > 0 } next ? Proxied(next) : null;
        }

        return sources;
    }

    public async Task<ContactSyncBatch> FetchChangesAsync(
        SyncConnection connection, string sourceId, string? deltaToken, CancellationToken ct = default)
    {
        var contacts = new List<RemoteContact>();
        var deleted = new List<string>();
        var unreadable = new List<string>();

        // a stored token is the whole deltaLink, so it already carries the folder and the state
        var path = deltaToken is { Length: > 0 } ? Proxied(deltaToken) : DeltaPath(sourceId);

        string? deltaLink = null;
        var pages = 0;

        while (path is not null)
        {
            if (++pages > MaxPages)
                throw new ContactSyncException($"Graph returned more than {MaxPages} pages of contacts; refusing to truncate.");

            var json = await SendAsync(connection, path, ct, deltaToken is { Length: > 0 }, PageSize);
            var response = Deserialize<GraphDeltaResponse>(json);

            foreach (var element in response?.Value ?? [])
            {
                var id = Identifier(element);

                if (IsRemoved(element))
                {
                    if (id is not null) deleted.Add(id);
                    continue;
                }

                try
                {
                    if (Project(element, sourceId) is { } contact)
                        contacts.Add(contact);
                    else if (id is not null)
                        unreadable.Add(id);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    logger.LogWarning(e, "could not read the Graph contact {Id}", id);
                    if (id is not null) unreadable.Add(id);
                }
            }

            deltaLink = response?.DeltaLink ?? deltaLink;
            path = response?.NextLink is { Length: > 0 } next ? Proxied(next) : null;
        }

        return new ContactSyncBatch(contacts, deleted, deltaLink, IsFullSync: deltaToken is not { Length: > 0 }, unreadable);
    }

    private static string CollectionPath(string sourceId) =>
        sourceId is DefaultSourceId or ""
            ? "me/contacts"
            : $"me/contactFolders/{Uri.EscapeDataString(sourceId)}/contacts";

    // Change tracking on contacts rejects $select, $top and friends outright, unlike every other
    // delta resource, so page size is negotiated with a Prefer header and we take the full entity.
    internal static string DeltaPath(string sourceId) => $"{Host}/v1.0/{CollectionPath(sourceId)}/delta";

    // Graph answers absolute next/delta links, and everything outbound goes through the proxy
    private static string Proxied(string absoluteUrl)
    {
        var uri = new Uri(absoluteUrl);
        return $"{uri.Host}{uri.AbsolutePath}{uri.Query}";
    }

    private async Task<string> SendAsync(SyncConnection connection, string path, CancellationToken ct,
                                         bool usingDeltaToken = false, int? maxPageSize = null)
    {
        using var request = proxy.Request(HttpMethod.Get, ServiceName, path, connection);

        if (maxPageSize is { } size)
            request.Headers.TryAddWithoutValidation("Prefer", $"odata.maxpagesize={size}");

        using var response = await proxy.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);

        var json = await response.Content.ReadAsStringAsync(ct);
        var status = (int)response.StatusCode;

        // Graph drops sync state after a while, and on a resource the token no longer covers
        if (status is 410 && usingDeltaToken)
            throw new DeltaTokenExpiredException("Graph no longer has sync state for this token; a full sync is required.");

        // adding Contacts to the Microsoft provider offers the capability to connections whose token
        // predates the scope, and the only fix is a relink
        if (status is 401 or 403)
            throw new ContactSyncException(
                "this Microsoft account needs relinking to grant contacts access");

        if (!response.IsSuccessStatusCode)
            throw new ContactSyncException(
                $"Graph {path} failed ({status}): {ConnectedServicesProxy.Truncate(json)}");

        return json;
    }

    private static T? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, Json);
        }
        catch (JsonException e)
        {
            throw new ContactSyncException($"Graph returned unreadable JSON: {e.Message}");
        }
    }

    private static bool IsRemoved(JsonElement element) => element.TryGetProperty("@removed", out _);

    private static string? Identifier(JsonElement element) =>
        element.TryGetProperty("id", out var id) ? id.GetString() : null;

    internal static RemoteContact? Project(JsonElement element, string sourceId = DefaultSourceId)
    {
        var contact = element.Deserialize<GraphContact>(Json);
        if (contact?.Id is not { Length: > 0 } id) return null;

        var item = new ContactItem();

        ContactSlots.Set(contact.GivenName, v => item.FirstName = v);
        ContactSlots.Set(contact.MiddleName, v => item.MiddleName = v);
        ContactSlots.Set(contact.Surname, v => item.LastName = v);
        ContactSlots.Set(contact.Title, v => item.Title = v);
        ContactSlots.Set(contact.Generation, v => item.Suffix = v);
        ContactSlots.Set(contact.NickName, v => item.NickName = v);

        ContactSlots.Set(contact.CompanyName, v => item.CompanyName = v);
        ContactSlots.Set(contact.Department, v => item.Department = v);
        ContactSlots.Set(contact.JobTitle, v => item.JobTitle = v);
        ContactSlots.Set(contact.OfficeLocation, v => item.OfficeLocation = v);

        ContactSlots.Set(contact.PersonalNotes, v => item.Notes = v);
        ContactSlots.Set(contact.BusinessHomePage, v => item.WebPage = v);
        ContactSlots.Set(contact.SpouseName, v => item.Spouse = v);

        ContactSlots.SetEmails(item, contact.EmailAddresses.Select(e => e.Address).OfType<string>());
        ContactSlots.SetImAddresses(item, contact.ImAddresses);
        ContactSlots.SetPhones(item, Phones(contact));
        ApplyAddresses(item, contact);

        foreach (var category in contact.Categories)
            if (!string.IsNullOrWhiteSpace(category))
                item.Categories.Add(new ContactCategory { Name = category });

        ContactSlots.Set(contact.FileAs, v => item.FileAs = v);
        if (!item.HasFileAs)
            ContactSlots.Set(contact.DisplayName, v => item.FileAs = v);
        if (!item.HasFileAs)
            ContactSlots.Set(ContactSlots.BuildFileAs(item), v => item.FileAs = v);

        if (Birthday(contact) is { } birthday)
            item.Birthday = Timestamp.FromDateTime(birthday);

        return new RemoteContact(id, item, contact.ETag, PhotoUrl(id, sourceId), PhotoServiceId: ServiceName);
    }

    private static IEnumerable<(PhoneSlot, string)> Phones(GraphContact contact)
    {
        if (contact.MobilePhone is { Length: > 0 } mobile)
            yield return (PhoneSlot.Mobile, mobile);

        foreach (var (number, index) in contact.HomePhones.Select((n, i) => (n, i)))
            yield return (index == 0 ? PhoneSlot.Home : PhoneSlot.Home2, number);

        foreach (var (number, index) in contact.BusinessPhones.Select((n, i) => (n, i)))
            yield return (index == 0 ? PhoneSlot.Business : PhoneSlot.Business2, number);
    }

    private static void ApplyAddresses(ContactItem item, GraphContact contact)
    {
        Apply(AddressSlot.Home, contact.HomeAddress);
        Apply(AddressSlot.Business, contact.BusinessAddress);
        Apply(AddressSlot.Other, contact.OtherAddress);

        void Apply(AddressSlot slot, GraphPhysicalAddress? address)
        {
            if (address is null || address.IsEmpty) return;

            ContactSlots.SetAddress(item, slot,
                address.Street, address.City, address.State, address.PostalCode, address.CountryOrRegion);
        }
    }

    private static DateTime? Birthday(GraphContact contact)
    {
        if (contact.Birthday is not { } birthday) return null;

        try
        {
            return ContactDates.Clamp(
                new DateTime(birthday.Year, birthday.Month, birthday.Day, 0, 0, 0, DateTimeKind.Utc));
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    // fetched through the proxy by ContactPhotoService, which is why this is a path and not a URL.
    // Addressed via the owning folder so it resolves the same way the contact itself was read.
    private static string PhotoUrl(string id, string sourceId) =>
        $"{Host}/v1.0/{CollectionPath(sourceId)}/{Uri.EscapeDataString(id)}/photo/$value";
}
