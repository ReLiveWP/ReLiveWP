using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Services.Push.Data;

public class ChannelStore(IDbContextFactory<PushDatabase> dbFactory)
{
    public async Task<PushChannel> RegisterAsync(
        string deviceId,
        string channelName,
        string serviceName,
        string version,
        string identifier,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // if we already have a connection we don't create a new one, this means channel URLs are stable
        // we might not actually want this in practice but for now it's convenient
        var existing = await db.Channels.FirstOrDefaultAsync(
            c => c.DeviceId == deviceId && c.ChannelName == channelName, ct);
        if (existing != null)
            return existing;

        var channel = new PushChannel
        {
            Token = Guid.NewGuid().ToString("N"),
            DeviceId = deviceId,
            ChannelName = channelName,
            ServiceName = serviceName,
            Version = version,
            Identifier = identifier,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        db.Channels.Add(channel);
        await db.SaveChangesAsync(ct); // Id assigned here
        return channel;
    }

    public async Task<PushChannel> FindByTokenAsync(string token, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Channels.FirstOrDefaultAsync(c => c.Token == token, ct);
    }

    public async Task<PushChannel> FindByIdAsync(long channelId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Channels.FirstOrDefaultAsync(c => c.ChannelId == channelId, ct);
    }

    public async Task<IReadOnlyList<PushChannel>> ForDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Channels.Where(c => c.DeviceId == deviceId).ToListAsync(ct);
    }
}
