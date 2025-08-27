using Grpc.Core;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Services;

public class ActivityProviderService(
    IServiceProvider serviceProvider,
    IHttpContextAccessor httpContextAccessor, 
    ConnectedServices.ConnectedServicesClient connectedServices)
{
    public async Task<ActivityProviderBase?> GetActivityProviderAsync()
    {
        var context = httpContextAccessor.HttpContext;
        if (context == null)
            return null;

        var auth = context.Request.Headers.Authorization.ToString();
        var authHeader = string.Concat("Bearer ", auth.AsSpan(auth.IndexOf(' ')));

        var headers = new Metadata() { { "Authorization", authHeader } };
        var servicesResponse = connectedServices.GetConnections(new ConnectionsRequest(), headers);

        List<BlueskyActivityProvider> providers = [];
        await foreach (var connection in servicesResponse.ResponseStream.ReadAllAsync())
        {
            if (connection.Service == "atproto")
            {
                providers.Add(ActivatorUtilities.CreateInstance<BlueskyActivityProvider>(serviceProvider, authHeader, connection));
            }
        }

        return new FeedCoalescingActivityProvider([.. providers]);
    }

}
