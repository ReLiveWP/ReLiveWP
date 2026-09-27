using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReLiveWP.ServiceDefaults.Outbound;

public static class FediverseJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
