using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Identity.Data;

namespace ReLiveWP.Backend.Identity.Services;

public class JWKProvider(
    LiveDbContext liveDbContext) : IJWKProvider
{
    public async Task<(string id, string key)> PickKeyAsync()
    {
        var keys = await liveDbContext.DPoPKeys.ToListAsync();
        var num = Random.Shared.Next(keys.Count);

        return (keys[num].Id, keys[num].Key);
    }

    public async Task<string> GetJWKAsync(string keyId)
    {
        var keys = await liveDbContext.DPoPKeys.FirstAsync(k => k.Id == keyId);
        return keys.Key;
    }
}
