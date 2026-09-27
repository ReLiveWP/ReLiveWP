namespace ReLiveWP.Services.Push.Nsp.Messages;

public class NspNotification
{
    public ushort MessageId { get; init; }
    public required uint ChannelId { get; init; }
    public required NspNotificationClass Class { get; init; }
    public required byte[] Payload { get; init; }

    public NspPackage ToPackage() => new()
    {
        Command = NspCommand.Notification,
        RequestId = MessageId,
        Tlvs =
        {
            NspTlv.UInt(NspTag.ChannelId, ChannelId),
            NspTlv.UInt((NspTag)0x29, (uint)Class),
        },
        TrailingData = Payload,
    };
}
