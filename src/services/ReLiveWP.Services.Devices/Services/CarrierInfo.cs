using System.Text.Json.Serialization;

namespace ReLiveWP.Services.Devices.Services;

public record struct CarrierInfo(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("countryName")] string? CountryName,
    [property: JsonPropertyName("countryCode")] string? CountryCode,
    [property: JsonPropertyName("mcc")] string MCC,
    [property: JsonPropertyName("mnc")] string MNC,
    [property: JsonPropertyName("brand")] string? Brand,
    [property: JsonPropertyName("operator")] string Operator,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("bands")] string Bands,
    [property: JsonPropertyName("notes")] string? Notes);
