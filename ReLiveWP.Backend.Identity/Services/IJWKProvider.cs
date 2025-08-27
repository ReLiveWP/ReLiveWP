
namespace ReLiveWP.Backend.Identity.Services;

public interface IJWKProvider
{
    Task<(string id, string key)> PickKeyAsync();
    Task<string> GetJWKAsync(string keyId);
}