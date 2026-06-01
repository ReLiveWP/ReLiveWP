namespace ReLiveWP.Services.Push.Pdu.Headers;

public class ErrorHeader : PDUHeader
{
    public override PDUHeaderType HeaderType => PDUHeaderType.Error;

    public ushort ErrorCode { get; set; }
    public uint ReferencedSequence { get; set; }

    // nextType(1) + length(2) + errorCode(2) + refSeq(4)
    public override int Length => 3 + 6;

    public override bool Read(BinaryReader reader, out PDUHeaderType nextType)
    {
        long start = reader.BaseStream.Position;
        nextType = (PDUHeaderType)reader.ReadByte();

        ushort length = reader.ReadUInt16();
        ErrorCode = reader.ReadUInt16();
        ReferencedSequence = reader.ReadUInt32();

        var consumed = reader.BaseStream.Position - start;
        reader.BaseStream.Position += (length + 3) - consumed;

        return true;
    }

    public override bool Write(BinaryWriter writer, PDUHeaderType nextType)
    {
        writer.Write((byte)nextType);
        writer.Write((ushort)(Length - 3));

        writer.Write(ErrorCode);
        writer.Write(ReferencedSequence);

        return true;
    }
}
