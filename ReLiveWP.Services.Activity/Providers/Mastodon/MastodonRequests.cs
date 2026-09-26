using System.Net.Http.Json;
using System.Text.Json;
using ReLiveWP.ServiceDefaults.Outbound;

namespace ReLiveWP.Services.Activity.Providers.Mastodon;

public static class MastodonRequests
{
    public static async Task<T?> GetJsonAsync<T>(HttpClient http, Uri url, ILogger logger, CancellationToken ct = default)
        where T : class
    {
        try
        {
            using var response = await http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation("{Url} answered {Status}", url, (int)response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<T>(FediverseJson.Options, ct);
        }
        catch (Exception ex) when ((ex is HttpRequestException or JsonException or TaskCanceledException) && !ct.IsCancellationRequested)
        {
            logger.LogInformation(ex, "could not read {Url}", url);
            return null;
        }
    }
}
