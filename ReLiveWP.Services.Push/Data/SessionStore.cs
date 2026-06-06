using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Services.Push.Data;

public class SessionStore(IDbContextFactory<PushDatabase> dbFactory)
{
    public async Task CreateAsync(string token, string deviceId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // one live session per device; a fresh Connect supersedes whatever came before
        await db.Sessions.Where(s => s.DeviceId == deviceId).ExecuteDeleteAsync(ct);

        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        db.Sessions.Add(new DeviceSession
        {
            Token = token,
            DeviceId = deviceId,
            CreatedAt = nowMs,
            LastSeenAt = nowMs,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<DeviceSession> FindAsync(string token, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Sessions.FirstOrDefaultAsync(s => s.Token == token, ct);
    }

    public async Task TouchAsync(string token, CancellationToken ct = default)
    {
        // TODO: last seen is kinda cute, we could consider poking this up the chain so we can show it in the UI
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await db.Sessions.Where(s => s.Token == token)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastSeenAt, now), ct);
    }
}
