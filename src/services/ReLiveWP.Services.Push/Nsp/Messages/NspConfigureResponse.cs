namespace ReLiveWP.Services.Push.Nsp.Messages;

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
