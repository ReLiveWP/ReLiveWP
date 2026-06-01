namespace ReLiveWP.Services.Push.Nsp.Messages;

public class NspConfigureRequest
{
    public ushort RequestId { get; init; }
    public uint ChannelId { get; init; }
    public IReadOnlyList<NspTlv> Options { get; init; } = [];

    public static NspConfigureRequest FromPackage(NspPackage package) => new()
    {
        RequestId = package.RequestId,
        ChannelId = package.GetUInt(NspTag.ChannelId) ?? 0,
        Options = package.Tlvs,
    };
}

public class NspConfigureResponse
{
    public required ushort RequestId { get; init; }
    public required IReadOnlyList<NspTlv> Options { get; init; }

    // so im not 100% on what these configuration options _do_ but the device needs them ACK'd 1:1 so
    // we send them on back
    public NspPackage ToPackage() => new()
    {
        Command = NspCommand.ConfigureResponse,
        RequestId = RequestId,
        Tlvs = [.. Options],
    };
}

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
