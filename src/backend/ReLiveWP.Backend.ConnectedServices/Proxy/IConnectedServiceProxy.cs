using ReLiveWP.Backend.ConnectedServices.Data;

namespace ReLiveWP.Backend.ConnectedServices.Proxy;

public interface IConnectedServiceProxy
{
    string ServiceId { get; }

    // Refresh the stored tokens for the given connection. Must be called while holding the per-connection lock.
    Task<bool> RefreshAsync(LiveConnectedService service, CancellationToken ct = default);

    // Forward an incoming HTTP request to the external service, signing it appropriately.
    // path is the remaining path after /proxy/{serviceId}/, e.g. "xrpc/app.bsky.feed.getAuthorFeed"
    Task SendProxiedRequestAsync(LiveConnectedService service, HttpContext context, string path, CancellationToken ct = default);
}
