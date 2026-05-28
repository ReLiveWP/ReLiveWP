namespace ReLiveWP.Backend.ConnectedServices.Services;

public interface IJWKProvider
{
    Task<(string id, string key)> PickKeyAsync();
    Task<string> GetJWKAsync(string keyId);
}
