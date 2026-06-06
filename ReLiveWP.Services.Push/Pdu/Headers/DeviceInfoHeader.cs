using System.Text;

namespace ReLiveWP.Services.Push.Pdu.Headers;

public class DeviceInfoHeader : PDUHeader
{
    public override PDUHeaderType HeaderType =>
        PDUHeaderType.DeviceInfo;

    public uint Unknown1 { get; set; }
    public uint Unknown2 { get; set; }
    public ushort Locale { get; set; }
    public byte[] Blob { get; set; }
    public string MaybeCarrier { get; set; }
    public string Block3 { get; set; }
    public string OSVersion { get; set; }
    public string MaybePhoneNumber { get; set; }
    public byte Unknown3 { get; set; }

    public override int Length =>
        3 + // header
        4 + 4 + 2 + // fixed fields
        (1 + 2 + Blob.Length) +
        (1 + 2 + MaybeCarrier.Length) +
        (1 + 2 + Block3.Length) +
        (1 + 2 + OSVersion.Length) +
        (1 + 2 + 14) +
        (1 + 2 + 1);

    public override bool Read(BinaryReader reader, out PDUHeaderType nextType)
    {
        var startOffset = reader.BaseStream.Position;
        nextType = (PDUHeaderType)reader.ReadByte();

        ushort payloadLen = reader.ReadUInt16();

        // fixed fields
        Unknown1 = reader.ReadUInt32();
        Unknown2 = reader.ReadUInt32();
        Locale = reader.ReadUInt16();

        // TLV loop (manually unrolled from binary)
        while (reader.BaseStream.Position - startOffset < payloadLen + 3)
        {
            byte tag = reader.ReadByte();
            ushort len = reader.ReadUInt16();

            switch (tag)
            {
                case 0x01:
                    Blob = reader.ReadBytes(len);
                    break;

                case 0x02:
                    MaybeCarrier = Encoding.UTF8.GetString(reader.ReadBytes(len));
                    break;

                case 0x03:
                    Block3 = Encoding.UTF8.GetString(reader.ReadBytes(len));
                    break;

                case 0x04:
                    OSVersion = Encoding.UTF8.GetString(reader.ReadBytes(len));
                    break;

                case 0x05:
                    MaybePhoneNumber = Encoding.UTF8.GetString(reader.ReadBytes(14));
                    break;

                case 0x06:
                    Unknown3 = reader.ReadByte();
                    break;

                default:
                    reader.BaseStream.Seek(len, SeekOrigin.Current);
                    break;
            }
        }

        return true;
    }

    public override bool Write(BinaryWriter writer, PDUHeaderType nextType)
    {
        throw new NotImplementedException();
    }
}
