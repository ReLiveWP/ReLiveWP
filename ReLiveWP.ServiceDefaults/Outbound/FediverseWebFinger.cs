using System.Net.Http.Json;
using System.Text.Json;

namespace ReLiveWP.ServiceDefaults.Outbound;

public static class FediverseWebFinger
{
    private const int MaxRedirects = 3;

    public static async Task<Uri?> FindActorUriAsync(HttpClient http, string accountAddress, CancellationToken ct = default)
    {
        if (!FediverseHandle.TryParse(accountAddress, out var handle) || handle.AccountAddress is not { } address)
            return null;

        var document = await FetchDocumentAsync(http, handle.DomainRoot, $"acct:{address}", ct);
        return FindActor(document);
    }

    // only answers when the server names the same actor back, so nobody can hand us someone else's handle
    public static async Task<string?> FindAccountAddressAsync(HttpClient http, Uri actorUri, CancellationToken ct = default)
    {
        if (!FediverseRequestGuard.IsAcceptableUri(actorUri))
            return null;

        var document = await FetchDocumentAsync(http, FediverseRequestGuard.GetInstanceRoot(actorUri), actorUri.AbsoluteUri, ct);
        if (document?.Subject is not { } subject || FindActor(document) != actorUri)
            return null;

        const string AcctScheme = "acct:";
        if (!subject.StartsWith(AcctScheme, StringComparison.OrdinalIgnoreCase))
            return null;

        return FediverseHandle.TryParse(subject[AcctScheme.Length..], out var handle) ? handle.AccountAddress : null;
    }

    private static async Task<WebFingerDocument?> FetchDocumentAsync(HttpClient http, Uri domainRoot, string resource, CancellationToken ct)
    {
        var url = new Uri(domainRoot, $"/.well-known/webfinger?resource={Uri.EscapeDataString(resource)}");

        try
        {
            for (var hop = 0; hop <= MaxRedirects; hop++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Accept.ParseAdd("application/jrd+json");
                request.Headers.Accept.ParseAdd("application/json");

                using var response = await http.SendAsync(request, ct);

                if ((int)response.StatusCode is >= 300 and < 400)
                {
                    if (response.Headers.Location is not { } location)
                        return null;

                    url = new Uri(url, location);
                    if (!FediverseRequestGuard.IsAcceptableUri(url))
                        return null;

                    continue;
                }

                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<WebFingerDocument>(ct);
            }
        }
        catch (Exception ex) when ((ex is HttpRequestException or JsonException or TaskCanceledException) && !ct.IsCancellationRequested)
        {
            return null;
        }

        return null;
    }

    private static Uri? FindActor(WebFingerDocument? document)
    {
        var href = document?.Links?.FirstOrDefault(IsActorLink)?.Href;
        return FediverseRequestGuard.TryParseAcceptableUri(href, out var actor) ? actor : null;
    }

    private static bool IsActorLink(WebFingerLink link)
        => link.Rel == "self" &&
           link.Type is { } type &&
           (type.StartsWith("application/activity+json", StringComparison.OrdinalIgnoreCase) ||
            type.StartsWith("application/ld+json", StringComparison.OrdinalIgnoreCase));

    private sealed record WebFingerDocument(string? Subject, WebFingerLink[]? Links);

    private sealed record WebFingerLink(string? Rel, string? Type, string? Href);
}
