using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

internal sealed record GooglePeopleResponse
{
    public List<JsonElement> Connections { get; init; } = [];
    public string? NextPageToken { get; init; }
    public string? NextSyncToken { get; init; }
}

internal sealed record GooglePerson
{
    public string? ResourceName { get; init; }
    public string? Etag { get; init; }

    public List<GoogleName> Names { get; init; } = [];
    public List<GoogleValue> Nicknames { get; init; } = [];
    public List<GoogleValue> EmailAddresses { get; init; } = [];
    public List<GooglePhone> PhoneNumbers { get; init; } = [];
    public List<GoogleAddress> Addresses { get; init; } = [];
    public List<GoogleOrganization> Organizations { get; init; } = [];
    public List<GoogleValue> Biographies { get; init; } = [];
    public List<GoogleValue> Urls { get; init; } = [];
    public List<GoogleBirthday> Birthdays { get; init; } = [];
    public List<GoogleRelation> Relations { get; init; } = [];
    public List<GoogleImClient> ImClients { get; init; } = [];
    public List<GooglePhoto> Photos { get; init; } = [];
}

internal sealed record GoogleName
{
    public string? GivenName { get; init; }
    public string? MiddleName { get; init; }
    public string? FamilyName { get; init; }
    public string? HonorificPrefix { get; init; }
    public string? HonorificSuffix { get; init; }
    public string? DisplayName { get; init; }
}

internal sealed record GoogleValue
{
    public string? Value { get; init; }
    public string? Type { get; init; }
}

internal sealed record GooglePhone
{
    public string? Value { get; init; }
    public string? Type { get; init; }
}

internal sealed record GoogleAddress
{
    public string? Type { get; init; }
    public string? StreetAddress { get; init; }
    public string? City { get; init; }
    public string? Region { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
}

internal sealed record GoogleOrganization
{
    public string? Name { get; init; }
    public string? Title { get; init; }
    public string? Department { get; init; }
    public string? Location { get; init; }
}

internal sealed record GoogleBirthday
{
    public GoogleDate? Date { get; init; }
}

// year is optional in the People API and EAS has nowhere to put a birthday without one
internal sealed record GoogleDate
{
    public int? Year { get; init; }
    public int? Month { get; init; }
    public int? Day { get; init; }
}

internal sealed record GoogleRelation
{
    public string? Person { get; init; }
    public string? Type { get; init; }
}

internal sealed record GoogleImClient
{
    public string? Username { get; init; }
}

internal sealed record GooglePhoto
{
    public string? Url { get; init; }

    [JsonPropertyName("default")]
    public bool Default { get; init; }
}
