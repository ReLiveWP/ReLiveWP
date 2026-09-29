using System.Buffers.Binary;
using System.Text;

namespace ReLiveWP.Services.Messenger.Msnp;

public readonly record struct MsnpBlpHeader(byte Flags, ushort PayloadLength, uint Sequence, uint? Ack, uint? Nak)
{
    private const byte SynFlag = 0x01;
    private const byte RequestAckFlag = 0x02;
    private const byte PeerInfoOption = 1;
    private const byte AckOption = 2;
    private const byte NakOption = 3;
    private const int FixedLength = 8;

    public bool IsSyn => (Flags & SynFlag) != 0;
    public bool RequestsAck => (Flags & RequestAckFlag) != 0;

    public override string ToString()
    {
        var text = new StringBuilder($"sn {Sequence} len {PayloadLength}");
        if (IsSyn)
            text.Append(" syn");
        if (RequestsAck)
            text.Append(" rak");
        if (Ack is { } ack)
            text.Append($" ack {ack}");
        if (Nak is { } nak)
            text.Append($" nak {nak}");

        return text.ToString();
    }

    public static bool TryRead(ReadOnlySpan<byte> sdgPayload, out MsnpBlpHeader header)
    {
        header = default;

        if (!MsnpSdg.TryFindMessagingHeaders(sdgPayload, out var headersStart, out var headersLength))
            return false;

        var body = sdgPayload[MsnpSdg.MessagingBodyStart(headersStart, headersLength)..];
        if (body.Length < FixedLength || body[0] < FixedLength || body[0] > body.Length)
            return false;

        var (ack, nak) = ReadFirstOption(body[FixedLength..body[0]]);
        header = new MsnpBlpHeader(
            body[1],
            BinaryPrimitives.ReadUInt16BigEndian(body[2..]),
            BinaryPrimitives.ReadUInt32BigEndian(body[4..]),
            ack,
            nak);
        return true;
    }

    private static (uint? Ack, uint? Nak) ReadFirstOption(ReadOnlySpan<byte> options)
    {
        for (var at = 0; at < options.Length; at++)
        {
            switch (options[at])
            {
                case PeerInfoOption:
                    return (null, null);
                case AckOption when at + 6 <= options.Length:
                    return (BinaryPrimitives.ReadUInt32BigEndian(options[(at + 2)..]), null);
                case NakOption when at + 6 <= options.Length:
                    return (null, BinaryPrimitives.ReadUInt32BigEndian(options[(at + 2)..]));
            }
        }

        return (null, null);
    }
}
