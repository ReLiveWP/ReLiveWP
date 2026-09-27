namespace ReLiveWP.Services.Push.Pdu.Headers;

public class UnknownPDUHeader : PDUHeader
{
    public PDUHeaderType ActualType { get; set; }

    public override PDUHeaderType HeaderType => ActualType;

    public byte[] Body { get; set; } = [];

    public override int Length => 3 + Body.Length;

    public override bool Read(BinaryReader reader, out PDUHeaderType nextType)
    {
        nextType = (PDUHeaderType)reader.ReadByte();
        ushort length = reader.ReadUInt16();
        Body = reader.ReadBytes(length);
        return Body.Length == length;
    }

    public override bool Write(BinaryWriter writer, PDUHeaderType nextType)
    {
        writer.Write((byte)nextType);
        writer.Write((ushort)Body.Length);
        writer.Write(Body);
        return true;
    }
}
