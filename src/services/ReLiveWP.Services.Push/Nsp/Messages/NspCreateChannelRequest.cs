namespace ReLiveWP.Services.Push.Nsp.Messages;

public class NspCreateChannelRequest
{
    // most of these names are best guesses, in WP7 you pass 2 strings to HttpNotificationChannel
    // channelName and serviceName strings, but it's not 100% clear yet which is which

    public ushort RequestId { get; init; }
    public string ChannelName { get; init; }
    public string Version { get; init; }
    public string ServiceName { get; init; }
    public string Identifier { get; init; }

    public static NspCreateChannelRequest FromPackage(NspPackage package) => new()
    {
        RequestId = package.RequestId,
        ChannelName = package.GetString(NspTag.Name),
        Version = package.GetString(NspTag.Version),
        ServiceName = package.GetString(NspTag.Publisher),
        Identifier = package.GetString(NspTag.Identifier),
    };
}
