namespace ReLiveWP.Services.Push.Pdu.Headers;

public class TransportSessionConfigHeader : PDUHeader
{
    public override PDUHeaderType HeaderType =>
        PDUHeaderType.TransportSessionConfig;

    public ushort TransportConfig { get; set; }

    public override int Length => 3 + 2;

    public override bool Read(BinaryReader reader, out PDUHeaderType nextType)
    {
        long start = reader.BaseStream.Position;
        nextType = (PDUHeaderType)reader.ReadByte();

        ushort length = reader.ReadUInt16();
        TransportConfig = reader.ReadUInt16();

        return (reader.BaseStream.Position - start) == length + 3;
    }

    public override bool Write(BinaryWriter writer, PDUHeaderType nextType)
    {
        writer.Write((byte)nextType);
        writer.Write((ushort)(Length - 3));

        writer.Write(TransportConfig);

        return true;
    }
}
