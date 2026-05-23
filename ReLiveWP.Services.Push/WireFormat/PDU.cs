using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace ReLiveWP.Services.Push.WireFormat;

public enum PDUHeaderType : byte
{
    End = 0,
    SessionInfo = 0x1,
    Authenticate = 0x3,
    DeviceInfo = 0x4,
    Sequence = 0x5,
    SessionConfig = 0x6,
    Binding = 0x12,
    Timestamp = 0x14,
    Signature = 0x15,
    NetworkInfo = 0x16,
    KeepAlive = 0x17,
    TransportSessionConfig = 0x18,
    DataHint3 = 0x19,
    Error = 0xFE
}

public abstract class PDUHeader
{
    public abstract PDUHeaderType HeaderType { get; }
    public abstract int Length { get; }
    public abstract bool Read(BinaryReader reader, out PDUHeaderType nextType);
    public abstract bool Write(BinaryWriter writer, PDUHeaderType nextType);
}

public class PDU
{
    public byte Unk1 { get; set; }
    public int Unk2 { get; set; }
    public PDUHeader[] Headers { get; set; }
    public byte[] Data { get; set; }

    private PDU() { }
    public PDU(byte unk1, int unk2, PDUHeader[] headers, byte[] data)
    {
        Unk1 = unk1;
        Unk2 = unk2;
        Headers = headers;
        Data = data;
    }

    public byte[] Serialize()
    {
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);

        writer.Write((byte)0x11);
        writer.Write(Unk1);

        var sizeOffset = stream.Position;
        writer.Write(short.MinValue); // to be filled

        writer.Write(Unk2);

        for (int i = 0; i < Headers.Length; i++)
        {
            var next = Headers.ElementAtOrDefault(i + 1);
            var nextType = next?.HeaderType ?? PDUHeaderType.End;

            Headers[i].Write(writer, nextType);
        }

        writer.Write(Data);

        var len = stream.Position;
        stream.Position = sizeOffset;
        writer.Write((short)(len - 9));

        return stream.ToArray();
    }

    public static bool TryParse(byte[] data, int offset, int length, out PDU result)
    {
        result = new PDU();

        var headers = new List<PDUHeader>();
        using var reader = new BinaryReader(new MemoryStream(data, offset, length));

        var magic = reader.ReadByte();
        if (magic != 0x11)
        {
            // bad magic
            return false;
        }

        result.Unk1 = reader.ReadByte();
        var len = reader.ReadInt16();
        if (len + 9 != reader.BaseStream.Length)
        {
            // bad size
            return false;
        }

        result.Unk2 = reader.ReadInt32();

        var headerType = (PDUHeaderType)reader.ReadByte();
        while (headerType != PDUHeaderType.End)
        {
            var header = headerType switch
            {
                PDUHeaderType.Authenticate => new AuthenticateHeader(),
                PDUHeaderType.SessionInfo => new SessionInfoHeader(),
                PDUHeaderType.SessionConfig => new SessionConfigHeader(),
                PDUHeaderType.TransportSessionConfig => new TransportSessionConfigHeader(),
                PDUHeaderType.DeviceInfo => new DeviceInfoHeader(),
                PDUHeaderType.NetworkInfo => new NetworkInfoHeader(),
                PDUHeaderType.Timestamp => new TimestampHeader(),
                PDUHeaderType.Signature => new SignatureHeader(),
                PDUHeaderType.Sequence => new SequenceHeader(),
                PDUHeaderType.KeepAlive => new OptimalKeepAliveHeader(),

                _ => new UnknownPDUHeader() as PDUHeader
            };

            if (!header.Read(reader, out headerType))
                return false;

            Console.WriteLine(headerType);
            headers.Add(header);
        }

        result.Headers = headers.ToArray();
        result.Data = reader.ReadBytes(data.Length - (int)reader.BaseStream.Position);

        return true;
    }
}
