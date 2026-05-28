namespace ReLiveWP.Backend.ConnectedServices.Services;

public interface IClientAssertionService
{
    Task<string> CreateClientAssertionAsync(string clientId, string issuer, string keyId);
}
