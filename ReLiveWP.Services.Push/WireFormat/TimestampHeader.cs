using System;
using System.IO;
using System.Text;

namespace ReLiveWP.Services.Push.WireFormat;

public class TimestampHeader : PDUHeader
{
    public override PDUHeaderType HeaderType =>
        PDUHeaderType.Timestamp;

    private const int TimestampSize = 0x19;

    //public byte[] TimestampData { get; set; } = new byte[TimestampSize];

    public DateTimeOffset Timestamp { get; set; }

    public override int Length => 3 + 1 + 2 + TimestampSize;

    public override bool Read(BinaryReader reader, out PDUHeaderType nextType)
    {
        long start = reader.BaseStream.Position;
        nextType = (PDUHeaderType)reader.ReadByte();

        ushort length = reader.ReadUInt16();

        byte tag = reader.ReadByte();
        if (tag != 0x01)
            return false;

        ushort tsLen = reader.ReadUInt16();
        if (tsLen != TimestampSize)
            return false;

        var timestampData = Encoding.UTF8.GetString(reader.ReadBytes(tsLen));
        Timestamp = DateTimeOffset.Parse(timestampData);

        return (reader.BaseStream.Position - start) == length + 3;
    }

    public override bool Write(BinaryWriter writer, PDUHeaderType nextType)
    {
        // pushsvc.dll has no parsing code
        throw new NotImplementedException();
    }
}
