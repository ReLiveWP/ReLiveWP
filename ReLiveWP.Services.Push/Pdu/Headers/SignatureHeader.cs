namespace ReLiveWP.Services.Push.Pdu.Headers;

public class SignatureHeader : PDUHeader
{
    public override PDUHeaderType HeaderType =>
        PDUHeaderType.Signature;

    public byte SignatureKind { get; set; }

    public byte[] Signature { get; set; }

    public override int Length =>
        3 +          // header
        1 +          // signature type
        1 + 2 +      // tag + length
        Signature.Length;

    public override bool Read(BinaryReader reader, out PDUHeaderType nextType)
    {
        long start = reader.BaseStream.Position;

        nextType = (PDUHeaderType)reader.ReadByte();

        ushort payloadLen = reader.ReadUInt16();

        SignatureKind = reader.ReadByte();

        byte tag = reader.ReadByte();
        if (tag != 0x01)
            return false;

        ushort sigLen = reader.ReadUInt16();

        Signature = reader.ReadBytes(sigLen);

        return (reader.BaseStream.Position - start) == payloadLen + 3;
    }

    public override bool Write(BinaryWriter writer, PDUHeaderType nextType)
    {
        // pushsvc.dll has no parsing code
        throw new NotImplementedException();
    }
}
