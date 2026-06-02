namespace ReLiveWP.Services.Push.Pdu.Headers;

public class SessionConfigHeader : PDUHeader
{
    public override PDUHeaderType HeaderType =>
        PDUHeaderType.SessionConfig;

    public ushort MaxPayloadSize { get; set; }
    public byte WindowSize { get; set; }
    public ushort Unk0x18 { get; set; }
    public byte Unk0x1a { get; set; }
    public ushort Unk0x1c { get; set; }
    public ushort MinKeepAliveInterval { get; set; }
    public ushort MaxKeepAliveInterval { get; set; }
    public uint Unk0x24 { get; set; }

    // nextType(1) + length(2) + 16-byte body
    public override int Length => 3 + 16;

    public override bool Read(BinaryReader reader, out PDUHeaderType nextType)
    {
        var start = reader.BaseStream.Position;

        nextType = (PDUHeaderType)reader.ReadByte();

        var payloadLength = reader.ReadUInt16();
        if (payloadLength < Length - 3)
            return false;

        MaxPayloadSize = reader.ReadUInt16();
        WindowSize = reader.ReadByte();
        Unk0x18 = reader.ReadUInt16();
        Unk0x1a = reader.ReadByte();
        Unk0x1c = reader.ReadUInt16();
        MinKeepAliveInterval = reader.ReadUInt16();
        MaxKeepAliveInterval = reader.ReadUInt16();
        Unk0x24 = reader.ReadUInt32();

        // skip any trailing bytes the device's deserializer reads as a managed buffer
        var consumed = (int)(reader.BaseStream.Position - start);
        reader.BaseStream.Position += (payloadLength + 3) - consumed;

        return true;
    }

    public override bool Write(BinaryWriter writer, PDUHeaderType nextType)
    {
        writer.Write((byte)nextType);
        writer.Write((ushort)(Length - 3));

        writer.Write(MaxPayloadSize);
        writer.Write(WindowSize);
        writer.Write(Unk0x18);
        writer.Write(Unk0x1a);
        writer.Write(Unk0x1c);
        writer.Write(MinKeepAliveInterval);
        writer.Write(MaxKeepAliveInterval);
        writer.Write(Unk0x24);

        return true;
    }
}
