namespace ReLiveWP.Backend.Chat;

public class ChatOptions
{
    public const string SectionName = "Chat";

    public TimeSpan ActiveWindow { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan AudienceCacheTtl { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan AudienceRefreshDelay { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan SendDedupeWindow { get; set; } = TimeSpan.FromMinutes(10);
    public double MessageBurst { get; set; } = 30;
    public double MessageRefillPerSecond { get; set; } = 1;
    public double DataBurstBytes { get; set; } = 10 * 1024 * 1024;
    public double DataRefillBytesPerSecond { get; set; } = 1024 * 1024;

    public TimeSpan StoredMessageRetention { get; set; } = TimeSpan.FromDays(30);
    public int MaxStoredPerRecipient { get; set; } = 500;
    public int MaxStoredPerPair { get; set; } = 100;

    public int MaxQueuedPerEndpoint { get; set; } = 5000;
    public int MaxEndpointsPerUser { get; set; } = 8;
    public double PresenceBurst { get; set; } = 5;
    public double PresenceRefillPerSecond { get; set; } = 1.0 / 30;

    public TimeSpan PresenceRetryAfter => TimeSpan.FromSeconds(1 / PresenceRefillPerSecond);
}
