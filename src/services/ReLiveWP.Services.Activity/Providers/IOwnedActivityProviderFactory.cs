using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Providers;

public interface IOwnedActivityProviderFactory
{
    string IdentityProvider { get; }

    OwnedActivityProviderBase Create(string userId, Connection connection);
}
