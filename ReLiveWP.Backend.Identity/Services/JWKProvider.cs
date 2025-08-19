namespace ReLiveWP.Backend.Identity.Services;

public class JWKProvider(IConfiguration configuration) : IJWKProvider
{
    public async Task<string> GetJWK(string keyId)
    {
        return configuration["AtProtoOAuth:JWK"]
                ?? throw new InvalidOperationException("No JsonWebKeys have been configured. This is bad!");
    }
}
