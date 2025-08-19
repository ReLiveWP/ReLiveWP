using ReLiveWP.Backend.Identity.Data;

namespace ReLiveWP.Backend.Identity.ConnectedServices;

public interface IOAuthProvider
{
    Task<LivePendingOAuth> BeginAccountLinkAsync(LiveUser user, string identifier);
    Task<LiveConnectedService> FinalizeAccountLinkAsync(LiveConnectedService connectedService, LivePendingOAuth state, string code);
    Task<bool> RefreshTokensAsync(LiveConnectedService connectedService);
}