using ReLiveWP.Backend.ConnectedServices.Data;

namespace ReLiveWP.Backend.ConnectedServices.OAuthProviders;

public interface IOAuthProvider
{
    Task<LivePendingOAuth> BeginAccountLinkAsync(Guid userId, string identifier);
    Task<LiveConnectedService> FinalizeAccountLinkAsync(LiveConnectedService connectedService, LivePendingOAuth state, string code);
    Task<bool> RefreshTokensAsync(LiveConnectedService connectedService);
}
