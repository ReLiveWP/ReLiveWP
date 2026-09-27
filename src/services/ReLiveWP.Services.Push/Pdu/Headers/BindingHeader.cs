using System.Text;

namespace ReLiveWP.Services.Push.Pdu.Headers;

public class BindingHeader : PDUHeader
{
    public override PDUHeaderType HeaderType => PDUHeaderType.Binding;

    public string Binding { get; set; } = string.Empty;

    private byte[] BindingBytes => Encoding.Latin1.GetBytes(Binding);

    // nextType(1) + length(2) + tag(1) + strLen(2) + string
    public override int Length => 3 + 3 + BindingBytes.Length;

    public override bool Read(BinaryReader reader, out PDUHeaderType nextType)
    {
        long start = reader.BaseStream.Position;
        nextType = (PDUHeaderType)reader.ReadByte();

        ushort length = reader.ReadUInt16();
        var tag = reader.ReadByte();
        if (tag != 0x01)
            return false;

        ushort strLen = reader.ReadUInt16();
        Binding = Encoding.Latin1.GetString(reader.ReadBytes(strLen));

        return (reader.BaseStream.Position - start) == length + 3;
    }

    public override bool Write(BinaryWriter writer, PDUHeaderType nextType)
    {
        var bytes = BindingBytes;

        writer.Write((byte)nextType);
        writer.Write((ushort)(Length - 3));

        writer.Write((byte)0x01);
        writer.Write((ushort)bytes.Length);
        writer.Write(bytes);

        return true;
    }
}
