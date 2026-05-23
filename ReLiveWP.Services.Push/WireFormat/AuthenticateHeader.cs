using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace ReLiveWP.Services.Push.WireFormat;

public class AuthenticateHeader : PDUHeader
{
    public override PDUHeaderType HeaderType =>
        PDUHeaderType.Authenticate;

    public override int Length { get; }

    public X509Certificate Certificate { get; private set; }
    public byte[] Blob { get; private set; }

    public override bool Read(BinaryReader reader, out PDUHeaderType nextType)
    {
        var startOffset = reader.BaseStream.Position;

        nextType = (PDUHeaderType)reader.ReadByte();

        var payloadLength = reader.ReadInt16() + 3;
        var tag = reader.ReadByte();

        if (tag != 1)
            return false;

        var blob1Size = reader.ReadInt16();
        var blob1 = reader.ReadBytes(blob1Size);

        tag = reader.ReadByte();
        if (tag != 2)
            throw new InvalidDataException();

        var blob2Size = reader.ReadInt16();
        var blob2 = reader.ReadBytes(blob2Size);

        var bytesRead = (reader.BaseStream.Position - startOffset);
        if (bytesRead != payloadLength)
            return false;

        var certificate = X509CertificateLoader.LoadCertificate(blob2);

        Blob = blob1;
        Certificate = certificate;

        return true;
    }

    public override bool Write(BinaryWriter writer, PDUHeaderType nextType)
    {
        throw new NotImplementedException();
    }
}
