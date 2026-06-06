namespace ReLiveWP.Services.Push.Nsp.Messages;

public class NspDeregisterResponse
{
    public required ushort RequestId { get; init; }
    public required uint ChannelId { get; init; }

    public NspPackage ToPackage() => new()
    {
        Command = NspCommand.DeregisterResponse,
        RequestId = RequestId,
        Tlvs = { NspTlv.UInt(NspTag.ChannelId, ChannelId) },
    };
}
