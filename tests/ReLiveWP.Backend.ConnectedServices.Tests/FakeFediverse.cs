using System.Net;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Providers;
using ReLiveWP.Backend.ConnectedServices.Services;
using ReLiveWP.ServiceDefaults.Outbound;

using ServiceCaps = ReLiveWP.Backend.ConnectedServices.Data.LiveConnectedServiceCapabilities;

namespace ReLiveWP.Backend.ConnectedServices.Tests;

public record RecordedRequest(HttpMethod Method, Uri Uri, string? Body, string? Authorization);

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

    public static HttpResponseMessage Redirect(string location)
        => new(HttpStatusCode.Found) { Headers = { Location = new Uri(location, UriKind.RelativeOrAbsolute) } };

    public IEnumerable<RecordedRequest> RequestsTo(string host)
        => Requests.Where(r => r.Uri.Host == host);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(ct);
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, body, request.Headers.Authorization?.ToString()));

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

public sealed class MastodonTestBed : IDisposable
{
    public const string RedirectUri = "https://login.test.relivewp.net/oauth/callback/mastodon";

    private readonly SqliteConnection connection;

    public MastodonTestBed()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var db = NewContext();
        db.Database.EnsureCreated();

        Description = new ConnectedServiceDescription
        {
            ServiceId = Mastodon.SERVICE_NAME,
            DisplayName = "Mastodon",
            RedirectUri = RedirectUri,
            Scopes = Mastodon.REQUESTED_SCOPES,
            ServiceCapabilities = ServiceCaps.SocialFeed | ServiceCaps.SocialPost | ServiceCaps.SocialPhotos,
        };

        Container = new ConnectedServicesContainer { [Mastodon.SERVICE_NAME] = Description };
        HttpClientFactory = new FakeHttpClientFactory(Server);

        var key = Convert.ToHexString(new byte[32]);
        Protector = new ConnectionSecretProtector(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ConnectionSecretProtector.KeyConfigPath] = key })
            .Build());
    }

    public FakeFediverseHandler Server { get; } = new();
    public FakeHttpClientFactory HttpClientFactory { get; }
    public ConnectedServiceDescription Description { get; }
    public ConnectedServicesContainer Container { get; }
    public ConnectionSecretProtector Protector { get; }

    public ConnectedServicesDbContext NewContext()
        => new(new DbContextOptionsBuilder<ConnectedServicesDbContext>().UseSqlite(connection).Options);

    public MastodonClientRegistry NewRegistry(ConnectedServicesDbContext db)
        => new(db, Container, Protector, HttpClientFactory, NullLogger<MastodonClientRegistry>.Instance);

    public MastodonOAuthProvider NewProvider(ConnectedServicesDbContext db)
        => new(NewRegistry(db), HttpClientFactory, NullLogger<MastodonOAuthProvider>.Instance);

    // strictly speaking these are from snug, so Mastodon API _compatible_ but fwiw that's fine
    public void ServeInstance(string host, string version = "4.2.1 (compatible; Iceshrimp 2026.5.1)")
        => Server.OnJson(HttpMethod.Get, $"https://{host}/api/v1/instance", $$"""{"uri":"{{host}}","version":"{{version}}"}""");

    public void ServeAppRegistration(string host, string clientId = "client-id", string clientSecret = "client-secret", long expiresAt = 0)
        => Server.OnJson(HttpMethod.Post, $"https://{host}/api/v1/apps",
            $$"""{"id":"1","name":"ReLiveWP","client_id":"{{clientId}}","client_secret":"{{clientSecret}}","client_secret_expires_at":{{expiresAt}}}""");

    public void ServeWebFinger(string domain, string account, string actorUri)
        => Server.OnJson(HttpMethod.Get,
            $"https://{domain}/.well-known/webfinger?resource=acct%3A{Uri.EscapeDataString(account)}",
            $$"""{"subject":"acct:{{account}}","links":[{"rel":"self","type":"application/activity+json","href":"{{actorUri}}"}]}""");

    public void Dispose() => connection.Dispose();
}
