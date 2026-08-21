using System.Net.Http.Headers;
using System.Text;
using ReLiveWP.Dav;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Backend.ConnectedServices.Services;

public static class DavCredentials
{
    public static AuthenticationHeaderValue BasicHeader(string? username, string? secret)
        => new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{secret}")));

    public static DavClient CreateClient(IHttpClientFactory factory, string? username, string? secret)
    {
        var client = factory.CreateClient(OutboundAddressPolicyExtensions.GuardedClientName);
        client.DefaultRequestHeaders.Authorization = BasicHeader(username, secret);

        return new DavClient(client);
    }
}
