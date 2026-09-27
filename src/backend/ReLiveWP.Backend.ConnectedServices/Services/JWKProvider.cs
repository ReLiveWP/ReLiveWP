using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ConnectedServices.Data;

namespace ReLiveWP.Backend.ConnectedServices.Services;

public class JWKProvider(ConnectedServicesDbContext dbContext) : IJWKProvider
{
    public async Task<(string id, string key)> PickKeyAsync()
    {
        var keys = await dbContext.DPoPKeys.ToListAsync();
        var num = Random.Shared.Next(keys.Count);
        return (keys[num].Id, keys[num].Key);
    }

    public async Task<string> GetJWKAsync(string keyId)
    {
        var key = await dbContext.DPoPKeys.FirstAsync(k => k.Id == keyId);
        return key.Key;
    }
}
