using System;
using System.IO;

namespace ReLiveWP.Services.Push.WireFormat;

public class UnknownPDUHeader : PDUHeader
{
    public override PDUHeaderType HeaderType =>
        0;

    public byte[] Data { get; set; }
    public override int Length { get; } = 3;

    public override bool Read(BinaryReader reader, out PDUHeaderType nextType)
    {
        nextType = (PDUHeaderType)reader.ReadByte();
        Data = reader.ReadBytes(Length);
        return true;
    }

    public override bool Write(BinaryWriter writer, PDUHeaderType nextType)
    {
        throw new NotImplementedException();
    }
}
