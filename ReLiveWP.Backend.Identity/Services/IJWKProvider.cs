
namespace ReLiveWP.Backend.Identity.Services;

public interface IJWKProvider
{
    Task<string> GetJWK(string keyId);
}