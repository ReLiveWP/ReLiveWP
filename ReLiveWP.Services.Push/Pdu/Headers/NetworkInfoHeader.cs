namespace ReLiveWP.Services.Push.Pdu.Headers;

public class NetworkInfoHeader : PDUHeader
{
    public override PDUHeaderType HeaderType => PDUHeaderType.NetworkInfo;
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

        // the network-name block is optional and only present on some transports (it shows up over
        // cellular carrying the apn, e.g. "data.mymeteor.ie", but not on wifi). presence is decided
        // by the declared length, not by whether our field happens to be set - on a fresh parse it
        // never is
        if (length <= 2)
            return (reader.BaseStream.Position - start) == length + 3;

        byte tag = reader.ReadByte();
        if (tag != 0x01)
            return false;

        ushort blobLen = reader.ReadUInt16();
        NetworkBlob = reader.ReadBytes(blobLen);

        return (reader.BaseStream.Position - start) == length + 3;
    }

    public override bool Write(BinaryWriter writer, PDUHeaderType nextType)
    {
        throw new NotImplementedException();
    }
}
