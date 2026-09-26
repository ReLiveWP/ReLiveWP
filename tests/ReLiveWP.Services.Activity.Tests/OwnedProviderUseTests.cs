using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Tests;

// a connection only feeds what's new when social feed is on, and only takes posts when social post is on
public class OwnedProviderUseTests
{
    private const uint SocialFeed = 0x20;
    private const uint SocialPost = 0x40;

    private readonly List<ConnectionsRequest> requests = [];
    private readonly ActivityProviderService service;

    public OwnedProviderUseTests()
    {
        var connectedServices = new FakeConnectedServicesClient
        {
            OnGetConnections = request =>
            {
                requests.Add(request);
                return [];
            },
        };

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "test")),
        };

        service = new ActivityProviderService(new HttpContextAccessor { HttpContext = context }, [], [],
            connectedServices, new FakeMailboxStoreClient(), TestCache.New(), NullLogger<ActivityProviderService>.Instance);
    }

    [Fact]
    public async Task Reading_asks_only_for_connections_with_social_feed_on()
    {
        await service.GetOwnedProviderAsync(OwnedProviderUse.Read);

        Assert.Equal(SocialFeed, Assert.Single(requests).Capabilities);
    }

    [Fact]
    public async Task Posting_asks_only_for_connections_with_social_post_on()
    {
        await service.GetOwnedProviderAsync(OwnedProviderUse.Post);

        Assert.Equal(SocialPost, Assert.Single(requests).Capabilities);
    }

    [Fact]
    public async Task Replies_are_read_with_social_feed()
    {
        await service.GetReplyProvidersAsync();

        Assert.Equal(SocialFeed, Assert.Single(requests).Capabilities);
    }
}
