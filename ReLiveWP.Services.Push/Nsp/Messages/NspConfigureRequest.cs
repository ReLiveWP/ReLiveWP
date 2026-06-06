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
