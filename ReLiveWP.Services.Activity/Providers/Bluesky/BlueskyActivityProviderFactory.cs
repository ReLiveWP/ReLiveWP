using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Providers.Bluesky;

public class BlueskyActivityProviderFactory(IConfiguration configuration, ILoggerFactory loggerFactory) : IOwnedActivityProviderFactory
{
    public string IdentityProvider => BlueskyEntryMapper.IdentityProviderToken;

    public OwnedActivityProviderBase Create(string userId, Connection connection)
        => new BlueskyActivityProvider(userId, connection, configuration, loggerFactory);
}
