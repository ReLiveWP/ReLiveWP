using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Activity.Providers.Mastodon;

namespace ReLiveWP.Services.Activity.Tests;

public record RecordedRequest(HttpMethod Method, Uri Uri, string? Body, IReadOnlyDictionary<string, string> Headers);

public class FakeFediverseHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpResponseMessage>> routes = new(StringComparer.Ordinal);

    public List<RecordedRequest> Requests { get; } = [];

    public void On(HttpMethod method, string url, Func<HttpResponseMessage> respond)
        => routes[$"{method} {url}"] = respond;

    public void OnJson(HttpMethod method, string url, string json, HttpStatusCode status = HttpStatusCode.OK)
        => On(method, url, () => Json(json, status));

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(ct);
        var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, body, headers));

        return routes.TryGetValue($"{request.Method} {request.RequestUri!.AbsoluteUri}", out var respond)
            ? respond()
            : new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}

public class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public List<string> RequestedNames { get; } = [];

    public HttpClient CreateClient(string name)
    {
        RequestedNames.Add(name);
        return new HttpClient(handler, disposeHandler: false);
    }
}

public sealed class MastodonFixture
{
    public const string SnugActor = "https://snug.moe/users/8zlkadc6ao";
    public const string ProxyBase = "https://connectedservices.test/proxy/mastodon/";

    public FakeFediverseHandler Server { get; } = new();
    public FakeHttpClientFactory HttpClientFactory { get; }
    public MastodonActorResolver Resolver { get; }
    public PublicMastodonActivityProvider PublicProvider { get; }

    public MastodonFixture()
    {
        HttpClientFactory = new FakeHttpClientFactory(Server);
        Resolver = new MastodonActorResolver(HttpClientFactory, TestCache.New(), NullLogger<MastodonActorResolver>.Instance);
        PublicProvider = new PublicMastodonActivityProvider(Resolver, HttpClientFactory, TestCache.New(),
            NullLogger<PublicMastodonActivityProvider>.Instance);
    }

    // again based on real snug captures, strictly speaking Mastodon _compatible_ 

    public static string Account(string id, string username, string? uri = null, string? fqn = null, string? acct = null,
                                 string displayName = "", string avatar = "https://snug.moe/files/avatar")
    {
        var uriField = uri == null ? "" : $",\"uri\":\"{uri}\"";
        var fqnField = fqn == null ? "" : $",\"fqn\":\"{fqn}\"";
        return $$"""{"id":"{{id}}","username":"{{username}}","acct":"{{acct ?? username}}","display_name":"{{displayName}}","avatar":"{{avatar}}","url":"https://snug.moe/@{{username}}"{{uriField}}{{fqnField}}}""";
    }

    public static string Status(string id, string account, string content = "<p>hello</p>", string visibility = "public",
                                string? inReplyTo = null, string? reblog = null, string media = "[]", string spoiler = "",
                                bool sensitive = false)
    {
        var replyField = inReplyTo == null ? "null" : $"\"{inReplyTo}\"";
        var sensitiveField = sensitive ? "true" : "false";
        return $$"""
            {"id":"{{id}}","created_at":"2026-09-25T12:00:00Z","uri":"https://snug.moe/notes/{{id}}","url":"https://snug.moe/notes/{{id}}",
             "content":{{System.Text.Json.JsonSerializer.Serialize(content)}},"spoiler_text":"{{spoiler}}","visibility":"{{visibility}}",
             "in_reply_to_id":{{replyField}},"reblog":{{reblog ?? "null"}},"replies_count":2,"account":{{account}},"media_attachments":{{media}},
             "sensitive":{{sensitiveField}}}
            """;
    }

    public static string Attachment(string id, string type = "image", string? url = null, string? previewUrl = null,
                                    string? description = null, int? width = null, int? height = null)
    {
        var urlField = System.Text.Json.JsonSerializer.Serialize(url ?? $"https://media.snug.moe/{id}.png");
        var previewField = System.Text.Json.JsonSerializer.Serialize(previewUrl ?? $"https://media.snug.moe/{id}-small.webp");
        var descriptionField = System.Text.Json.JsonSerializer.Serialize(description);
        var metaField = width == null ? "null" : $$$"""{"original":{"width":{{{width}}},"height":{{{height}}}}}""";
        return $$"""{"id":"{{id}}","type":"{{type}}","url":{{urlField}},"preview_url":{{previewField}},"description":{{descriptionField}},"meta":{{metaField}}}""";
    }

    public void ServeWebFingerByAcct(string domain, string address, string actor)
        => Server.OnJson(HttpMethod.Get,
            $"https://{domain}/.well-known/webfinger?resource=acct%3A{Uri.EscapeDataString(address)}",
            $$"""{"subject":"acct:{{address}}","links":[{"rel":"self","type":"application/activity+json","href":"{{actor}}"}]}""");

    public void ServeWebFingerByActor(string actor, string subject, string? selfLink = null)
        => Server.OnJson(HttpMethod.Get,
            $"{new Uri(actor).GetLeftPart(UriPartial.Authority)}/.well-known/webfinger?resource={Uri.EscapeDataString(actor)}",
            $$"""{"subject":"{{subject}}","links":[{"rel":"self","type":"application/activity+json","href":"{{selfLink ?? actor}}"}]}""");

    public void ServeSnugAccount()
    {
        ServeWebFingerByActor(SnugActor, "acct:wamwoowam@snug.moe");
        Server.OnJson(HttpMethod.Get, "https://snug.moe/api/v1/accounts/lookup?acct=wamwoowam%40snug.moe",
            Account("8zlkadc6ao", "wamwoowam", fqn: "wamwoowam@snug.moe", displayName: "Wam"));
    }
}
