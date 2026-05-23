using System.IO;

namespace ReLiveWP.Services.Push.WireFormat;

public class NetworkInfoHeader : PDUHeader
{
    public override PDUHeaderType HeaderType =>
        PDUHeaderType.NetworkInfo;

    public ushort NetworkField { get; set; }

    public byte[] NetworkBlob { get; set; }

    public override int Length =>
        3 + 2 +
        (NetworkBlob != null
            ? (1 + 2 + NetworkBlob.Length)
            : 0);

    public override bool Read(BinaryReader reader, out PDUHeaderType nextType)
    {
        long start = reader.BaseStream.Position;
        nextType = (PDUHeaderType)reader.ReadByte();

        ushort length = reader.ReadUInt16();
        NetworkField = reader.ReadUInt16();

        long consumed = reader.BaseStream.Position - start;
        if (NetworkBlob == null)
        {
            // no optional block
            return consumed == length + 3;
        }

        byte tag = reader.ReadByte();
        if (tag != 0x01)
            return false;

        ushort blobLen = reader.ReadUInt16();
        NetworkBlob = reader.ReadBytes(blobLen);

        return (reader.BaseStream.Position - start) == length + 3;
    }

    public override bool Write(BinaryWriter writer, PDUHeaderType nextType)
    {
        writer.Write((byte)PDUHeaderType.NetworkInfo);
        writer.Write((byte)nextType);

        ushort length = (ushort)(Length - 3);

        writer.Write(length);
        writer.Write(NetworkField);

        if (NetworkBlob != null && NetworkBlob.Length > 0)
        {
            writer.Write((byte)0x01);
            writer.Write((ushort)NetworkBlob.Length);
            writer.Write(NetworkBlob);
        }

        return true;
    }
}
