using System.Text.Json;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

internal static class GooglePeopleErrors
{
    // an expired or personFields-mismatched syncToken comes back FAILED_PRECONDITION, a corrupt one
    // INVALID_ARGUMENT. both only clear by dropping the token and pulling in full.
    internal static bool IsSyncTokenRejected(int status, string json)
    {
        if (status != 400) return false;

        try
        {
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Object) return false;
            if (!document.RootElement.TryGetProperty("error", out var error)) return false;
            if (error.ValueKind != JsonValueKind.Object) return false;
            if (!error.TryGetProperty("status", out var value)) return false;
            if (value.ValueKind != JsonValueKind.String) return false;

            return value.GetString() is "FAILED_PRECONDITION" or "INVALID_ARGUMENT";
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
