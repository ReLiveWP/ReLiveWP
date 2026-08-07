using Grpc.Core;
using ReLiveWP.Identity;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Services;

public class ActivityProviderService(
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

        var auth = context.User.Id()!;
        var servicesResponse = connectedServices.GetConnections(new ConnectionsRequest());

        const ulong BustedFlag = 0x80000000UL;

        List<BlueskyActivityProvider> providers = [];
        await foreach (var connection in servicesResponse.ResponseStream.ReadAllAsync())
        {
            if (connection.Service == "atproto" && (connection.Flags & BustedFlag) == 0)
            {
                providers.Add(new BlueskyActivityProvider(auth, connection, configuration, loggerFactory));
            }
        }

        return new FeedCoalescingActivityProvider([.. providers]);
    }

}
