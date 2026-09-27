namespace ReLiveWP.Services.Push.Nsp.Messages;

public class NspEndpointAck
{
    public required NspCommand RequestCommand { get; init; }
    public required ushort RequestId { get; init; }
    public required uint ChannelId { get; init; }

    public NspPackage ToPackage() => new()
    {
        Command = (NspCommand)((byte)RequestCommand | 0x80),
        RequestId = RequestId,
        Tlvs = { NspTlv.UInt(NspTag.ChannelId, ChannelId) },
    };
}
