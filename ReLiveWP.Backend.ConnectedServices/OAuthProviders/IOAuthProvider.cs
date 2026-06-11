using ReLiveWP.Backend.ConnectedServices.Data;

namespace ReLiveWP.Backend.ConnectedServices.OAuthProviders;

public interface IOAuthProvider
{
    Task<LivePendingOAuth> BeginAccountLinkAsync(Guid userId, string identifier);
    Task<bool> RefreshTokensAsync(LiveConnectedService connectedService);
    Task<LiveConnectedService> FinalizeAccountLinkAsync(LiveConnectedService connectedService, LivePendingOAuth state, string code);

    public virtual Task<LiveConnectedService> FinalizeAccountLinkAsync(LiveConnectedService connectedService, LivePendingOAuth state, string code, string[] authorizedScopes)
    {
        return this.FinalizeAccountLinkAsync(connectedService, state, code);
    }
}
