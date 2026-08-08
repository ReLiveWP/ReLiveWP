using ReLiveWP.Backend.ConnectedServices.Data;

namespace ReLiveWP.Backend.ConnectedServices.OAuthProviders;

public record CredentialLink(string ServiceUrl, string Username, string Secret, string? Label = null);

public class CredentialLinkException(string message) : Exception(message);

public interface ICredentialLinkProvider
{
    Task<LiveConnectedService> LinkAsync(LiveConnectedService connection, CredentialLink credentials, CancellationToken ct = default);
}
