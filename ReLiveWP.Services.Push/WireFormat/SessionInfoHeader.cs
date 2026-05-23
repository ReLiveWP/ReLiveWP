using System;
using System.IO;

namespace ReLiveWP.Services.Push.WireFormat;

public class SessionInfoHeader : PDUHeader
{
    public override PDUHeaderType HeaderType =>
        PDUHeaderType.SessionInfo;

    public override int Length { get; }

    public override bool Read(BinaryReader reader, out PDUHeaderType nextType)
    {
        var startOffset = reader.BaseStream.Position;
        nextType = (PDUHeaderType)reader.ReadByte();

        var payloadLength = reader.ReadInt16();
        var tag = reader.ReadByte();
        if (tag != 0x1)
            return false;

        var blobLength = reader.ReadInt16();
        if (blobLength > (payloadLength - 3))
            return false;

        var blob1 = reader.ReadBytes((int)blobLength);

        var consumed = (int)(reader.BaseStream.Position - startOffset);
        if (consumed > (payloadLength + 3))
            return false;

        var remaining = (payloadLength + 3) - consumed;

        var blob2 = reader.ReadBytes((int)blobLength);

        throw new NotImplementedException();
    }

    public override bool Write(BinaryWriter writer, PDUHeaderType nextType)
    {
        throw new NotImplementedException();
    }
}
