using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

internal sealed record GraphDeltaResponse
{
    public List<JsonElement> Value { get; init; } = [];

    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; init; }

    [JsonPropertyName("@odata.deltaLink")]
    public string? DeltaLink { get; init; }
}

internal sealed record GraphFoldersResponse
{
    public List<GraphContactFolder> Value { get; init; } = [];

    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; init; }
}

internal sealed record GraphContactFolder
{
    public string? Id { get; init; }
    public string? DisplayName { get; init; }
}

internal sealed record GraphContact
{
    public string? Id { get; init; }

    [JsonPropertyName("@odata.etag")]
    public string? ETag { get; init; }

    public string? DisplayName { get; init; }
    public string? GivenName { get; init; }
    public string? MiddleName { get; init; }
    public string? Surname { get; init; }

    // honorific prefix, not the job title: Graph keeps those in separate fields
    public string? Title { get; init; }
    public string? Generation { get; init; }

    public string? NickName { get; init; }
    public string? FileAs { get; init; }
    public string? CompanyName { get; init; }
    public string? Department { get; init; }
    public string? JobTitle { get; init; }
    public string? OfficeLocation { get; init; }
    public string? BusinessHomePage { get; init; }
    public string? PersonalNotes { get; init; }
    public string? SpouseName { get; init; }
    public DateTimeOffset? Birthday { get; init; }

    public List<string> Categories { get; init; } = [];
    public List<GraphEmailAddress> EmailAddresses { get; init; } = [];
    public List<string> HomePhones { get; init; } = [];
    public List<string> BusinessPhones { get; init; } = [];
    public string? MobilePhone { get; init; }
    public List<string> ImAddresses { get; init; } = [];

    public GraphPhysicalAddress? HomeAddress { get; init; }
    public GraphPhysicalAddress? BusinessAddress { get; init; }
    public GraphPhysicalAddress? OtherAddress { get; init; }
}

internal sealed record GraphEmailAddress
{
    public string? Name { get; init; }
    public string? Address { get; init; }
}

// Graph sends an empty object rather than omitting an unset address
internal sealed record GraphPhysicalAddress
{
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? CountryOrRegion { get; init; }
    public string? PostalCode { get; init; }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Street) && string.IsNullOrWhiteSpace(City) &&
        string.IsNullOrWhiteSpace(State) && string.IsNullOrWhiteSpace(CountryOrRegion) &&
        string.IsNullOrWhiteSpace(PostalCode);
}
