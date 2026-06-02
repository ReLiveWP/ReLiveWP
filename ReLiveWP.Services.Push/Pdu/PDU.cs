using System.Text;
using ReLiveWP.Services.Push.Pdu.Headers;

namespace ReLiveWP.Services.Push.Pdu;

public class PDU
{
    public PDUCommand Command { get; set; }
    public uint SequenceNumber { get; set; }

    public PDUHeader[] Headers { get; set; }
    public byte[] Data { get; set; }

    private PDU() { }

    public PDU(PDUCommand command, uint sequenceNumber, PDUHeader[] headers, byte[] data)
    {
        Command = command;
        SequenceNumber = sequenceNumber;
        Headers = headers;
        Data = data;
    }

    public byte[] Serialize()
    {
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);

        writer.Write((byte)0x11);        // magic / version
        writer.Write((byte)Command);     // command opcode

        var sizeOffset = stream.Position;
        writer.Write((ushort)0);         // body length, filled in below

        writer.Write(SequenceNumber);

        // the PDU header carries the FIRST header's type; every header then
        // writes the NEXT header's type (0x00 terminates the list).
        var firstType = Headers.Length > 0 ? Headers[0].HeaderType : PDUHeaderType.End;
        writer.Write((byte)firstType);

        for (int i = 0; i < Headers.Length; i++)
        {
            var nextType = (i + 1 < Headers.Length) ? Headers[i + 1].HeaderType : PDUHeaderType.End;
            Headers[i].Write(writer, nextType);
        }

        if (Data != null)
            writer.Write(Data);

        var total = stream.Position;
        stream.Position = sizeOffset;
        writer.Write((ushort)(total - 9));

        return stream.ToArray();
    }

    public static bool TryParse(byte[] data, int offset, int length, out PDU result)
    {
        result = new PDU();

        var headers = new List<PDUHeader>();
        using var reader = new BinaryReader(new MemoryStream(data, offset, length));

        var magic = reader.ReadByte();
        if (magic != 0x11)
        {
            // bad magic
            return false;
        }

        result.Command = (PDUCommand)reader.ReadByte();
        var len = reader.ReadInt16();
        if (len + 9 != reader.BaseStream.Length)
        {
            // bad size
            return false;
        }

        result.SequenceNumber = reader.ReadUInt32();

        var headerType = (PDUHeaderType)reader.ReadByte();
        while (headerType != PDUHeaderType.End)
        {
            var header = headerType switch
            {
                PDUHeaderType.Authenticate => new AuthenticateHeader(),
                PDUHeaderType.SessionInfo => new SessionInfoHeader(),
                PDUHeaderType.SessionConfig => new SessionConfigHeader(),
                PDUHeaderType.TransportSessionConfig => new TransportSessionConfigHeader(),
                PDUHeaderType.DeviceInfo => new DeviceInfoHeader(),
                PDUHeaderType.NetworkInfo => new NetworkInfoHeader(),
                PDUHeaderType.Timestamp => new TimestampHeader(),
                PDUHeaderType.Signature => new SignatureHeader(),
                PDUHeaderType.Sequence => new SequenceHeader(),
                PDUHeaderType.KeepAlive => new OptimalKeepAliveHeader(),
                PDUHeaderType.Binding => new BindingHeader(),
                PDUHeaderType.Error => new ErrorHeader(),

                _ => new UnknownPDUHeader { ActualType = headerType } as PDUHeader
            };

            if (!header.Read(reader, out headerType))
                return false;

            headers.Add(header);
        }

        result.Headers = headers.ToArray();
        result.Data = reader.ReadBytes(data.Length - (int)reader.BaseStream.Position);

        return true;
    }
}
