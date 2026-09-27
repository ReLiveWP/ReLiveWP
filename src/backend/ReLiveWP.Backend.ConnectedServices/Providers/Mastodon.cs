namespace ReLiveWP.Backend.ConnectedServices.Providers;

public static class Mastodon
{
    public const string SERVICE_NAME = "mastodon";
    public const string CLIENT_NAME = "ReLiveWP";
    public const string CLIENT_WEBSITE = "https://github.com/ReLiveWP/ReLiveWP";

    public const string REQUESTED_SCOPES = "read:accounts read:statuses write:statuses write:media";
    public const string FALLBACK_SCOPES = "read write";
}
