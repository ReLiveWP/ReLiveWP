using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace ReLiveWP.ServiceDefaults;

public readonly record struct ContentLocation(
    string Url,
    IReadOnlyDictionary<string, string> Headers,
    string ContentType,
    string? ETag);

public static class ContentStreaming
{
    public static async Task<HttpResponseMessage> FetchAsync(this HttpClient client,
                                                             ContentLocation location,
                                                             HttpContext context,
                                                             CancellationToken ct = default)
    {
        using var upstream = new HttpRequestMessage(HttpMethod.Get, location.Url);

        foreach (var (name, value) in location.Headers)
            upstream.Headers.TryAddWithoutValidation(name, value);

        if (context.Request.Headers.TryGetValue(HeaderNames.Range, out var range))
            upstream.Headers.TryAddWithoutValidation(HeaderNames.Range, range.ToString());

        return await client.SendAsync(upstream, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    public static async Task PipeAsync(this HttpResponseMessage response,
                                       ContentLocation location,
                                       HttpContext context,
                                       CancellationToken ct = default)
    {
        var target = context.Response;

        target.StatusCode = (int)response.StatusCode;
        target.ContentType = response.Content.Headers.ContentType?.MediaType ?? location.ContentType;

        if (response.Content.Headers.ContentLength is { } length)
            target.ContentLength = length;

        if (response.Content.Headers.ContentRange is { } contentRange)
            target.Headers[HeaderNames.ContentRange] = contentRange.ToString();

        if (!string.IsNullOrEmpty(location.ETag))
            target.Headers[HeaderNames.ETag] = location.ETag;

        target.Headers[HeaderNames.AcceptRanges] = "bytes";

        if (HttpMethods.IsHead(context.Request.Method))
            return;

        await response.Content.CopyToAsync(target.Body, ct);
    }
}
