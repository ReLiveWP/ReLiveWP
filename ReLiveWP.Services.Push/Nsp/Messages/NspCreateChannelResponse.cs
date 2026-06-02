namespace ReLiveWP.Services.Push.Nsp.Messages;

public class NspCreateChannelResponse
{
    public required ushort RequestId { get; init; }
    public required string ChannelUri { get; init; }
    public required uint ChannelId { get; init; }

    public NspPackage ToPackage() => new()
    {
        Command = NspCommand.CreateChannelResponse,
        RequestId = RequestId,
        Tlvs =
        {
            NspTlv.String(NspTag.ChannelUri, ChannelUri),
            NspTlv.UInt(NspTag.ChannelId, ChannelId),
        },
    };
}
