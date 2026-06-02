namespace ReLiveWP.Services.Push.Pdu.Headers;

public class SessionInfoHeader : PDUHeader
{
    public override PDUHeaderType HeaderType =>
        PDUHeaderType.SessionInfo;

    public byte[] Blob { get; set; } = [];

    public byte[] Blob2 { get; set; } = [];

    // nextType(1) + length(2) + tag(1) + blob1Len(2) + blob1 + blob2
    public override int Length => 3 + 3 + Blob.Length + Blob2.Length;

    public override bool Read(BinaryReader reader, out PDUHeaderType nextType)
    {
        var start = reader.BaseStream.Position;
        nextType = (PDUHeaderType)reader.ReadByte();

        var payloadLength = reader.ReadInt16();
        var tag = reader.ReadByte();
        if (tag != 0x1)
            return false;

        var blobLength = reader.ReadUInt16();
        if (blobLength > (payloadLength - 3))
            return false;

        Blob = reader.ReadBytes(blobLength);

        var consumed = (int)(reader.BaseStream.Position - start);
        var remaining = (payloadLength + 3) - consumed;
        if (remaining < 0)
            return false;

        Blob2 = reader.ReadBytes(remaining);

        return (reader.BaseStream.Position - start) == payloadLength + 3;
    }

    public override bool Write(BinaryWriter writer, PDUHeaderType nextType)
    {
        writer.Write((byte)nextType);
        writer.Write((ushort)(Length - 3));

        writer.Write((byte)0x01);
        writer.Write((ushort)Blob.Length);
        writer.Write(Blob);
        writer.Write(Blob2);

        return true;
    }
}
