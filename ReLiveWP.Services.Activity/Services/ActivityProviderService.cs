using Grpc.Core;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Services;

public class ActivityProviderService(
    IServiceProvider serviceProvider,
    IHttpContextAccessor httpContextAccessor, 
    IConfiguration configuration,
    ILoggerFactory loggerFactory,
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

        const ulong BustedFlag = 0x80000000UL;

        List<BlueskyActivityProvider> providers = [];
        await foreach (var connection in servicesResponse.ResponseStream.ReadAllAsync())
        {
            if (connection.Service == "atproto" && (connection.Flags & BustedFlag) == 0)
            {
                providers.Add(new BlueskyActivityProvider(authHeader, connection, configuration, loggerFactory));
            }
        }

        return new FeedCoalescingActivityProvider([.. providers]);
    }

}
