namespace ReLiveWP.Services.Push.Pdu.Headers;

public class SequenceHeader : PDUHeader
{
    public uint SequenceNumber { get; set; }

    public byte[] Payload { get; set; }

    public override int Length =>
        3 +                            // nextType + length prefix
        4 +                            // sequence
        (Payload?.Length ?? 0);

    public override PDUHeaderType HeaderType { get; }
        = PDUHeaderType.Sequence;

    public override bool Read(BinaryReader reader, out PDUHeaderType nextType)
    {
        long start = reader.BaseStream.Position;

        nextType = (PDUHeaderType)reader.ReadByte();

        ushort length = reader.ReadUInt16();

        if (length < 4)
            return false;

        SequenceNumber = reader.ReadUInt32();

        int remaining = length - 4;

        if (remaining > 0)
        {
            Payload = reader.ReadBytes(remaining);
        }

        return (reader.BaseStream.Position - start) == length + 3;
    }

    public override bool Write(BinaryWriter writer, PDUHeaderType nextType)
    {
        writer.Write((byte)nextType);
        writer.Write((ushort)(Length - 3));

        writer.Write(SequenceNumber);

        if (Payload != null)
            writer.Write(Payload);

        return true;
    }
}
