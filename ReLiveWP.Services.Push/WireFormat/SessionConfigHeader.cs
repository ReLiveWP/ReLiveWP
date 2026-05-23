using System;
using System.IO;

namespace ReLiveWP.Services.Push.WireFormat;

public class SessionConfigHeader : PDUHeader
{
    public override PDUHeaderType HeaderType =>
        PDUHeaderType.SessionConfig;

    public short Unk1 { get; private set; }
    public byte Unk2 { get; private set; }
    public short Unk3 { get; private set; }
    public byte Unk4 { get; private set; }
    public short Unk5 { get; private set; }
    public short Unk6 { get; private set; }
    public short MaxKeepAliveInterval { get; private set; }
    public short Unk8 { get; private set; }

    public override int Length { get; } = 19;

    public override bool Read(BinaryReader reader, out PDUHeaderType nextType)
    {
        var startOffset = reader.BaseStream.Position;

        nextType = (PDUHeaderType)reader.ReadByte();

        var payloadLength = reader.ReadInt16();
        var minLength = Length - 3;

        if (payloadLength < minLength)
            throw new InvalidDataException();

        Unk1 = reader.ReadInt16();
        Unk2 = reader.ReadByte();
        Unk3 = reader.ReadInt16();
        Unk4 = reader.ReadByte();
        Unk5 = reader.ReadInt16();
        Unk6 = reader.ReadInt16();
        MaxKeepAliveInterval = reader.ReadInt16();
        Unk8 = reader.ReadInt16();

        var bytesConsumed = (reader.BaseStream.Position - startOffset);
        var remaining = (payloadLength + 3) - bytesConsumed;

        reader.BaseStream.Position += remaining;

        return true;
    }

    public override bool Write(BinaryWriter writer, PDUHeaderType nextType)
    {
        throw new NotImplementedException();
    }
}
