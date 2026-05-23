using System.IO;

namespace ReLiveWP.Services.Push.WireFormat;

public class SequenceHeader : PDUHeader
{
    public uint SequenceNumber { get; set; }

    public byte[] Payload { get; set; }

    public override int Length =>
        3 +        // header
        4;        // sequence

    public override PDUHeaderType HeaderType { get; }
        = PDUHeaderType.Sequence;

    public override bool Read(BinaryReader reader, out PDUHeaderType nextType)
    {
        long start = reader.BaseStream.Position;

        nextType = (PDUHeaderType)reader.ReadByte();

        ushort length = reader.ReadUInt16();

        if (length <= 3)
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
        long start = writer.BaseStream.Position;

        writer.Write((byte)PDUHeaderType.Sequence);
        writer.Write((byte)nextType);

        writer.Write((ushort)(Length - 3));

        writer.Write(SequenceNumber);

        return true;
    }
}