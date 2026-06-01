namespace ReLiveWP.Services.Push.Nsp;

public class NspPackage
{
    public byte Version { get; set; } = 1;
    public NspCommand Command { get; set; }
    public ushort RequestId { get; set; }
    public List<NspTlv> Tlvs { get; set; } = [];
    public byte[] TrailingData { get; set; } = [];

    public NspTlv GetTlv(NspTag tag) => Tlvs.FirstOrDefault(t => t.Tag == tag);
    public string GetString(NspTag tag) => GetTlv(tag)?.AsString();
    public uint? GetUInt(NspTag tag) => GetTlv(tag)?.AsUInt();

    public byte[] Serialize()
    {
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        int tlvLen = Tlvs.Sum(t => t.WireLength);

        writer.Write(Version);
        writer.Write((byte)Command);
        writer.Write((uint)(tlvLen + TrailingData.Length)); // total payload
        writer.Write(RequestId);
        writer.Write((ushort)tlvLen);

        foreach (var tlv in Tlvs)
        {
            writer.Write((byte)tlv.Tag);
            writer.Write((ushort)tlv.Value.Length);
            writer.Write(tlv.Value);
        }

        writer.Write(TrailingData);

        return stream.ToArray();
    }

    public static bool TryParse(byte[] data, out NspPackage package)
    {
        package = new NspPackage();
        if (data == null || data.Length < 10)
            return false;

        using var reader = new BinaryReader(new MemoryStream(data));

        package.Version = reader.ReadByte();
        if (package.Version != 1)
            return false;

        package.Command = (NspCommand)reader.ReadByte();
        uint total = reader.ReadUInt32();
        package.RequestId = reader.ReadUInt16();
        ushort tlvLen = reader.ReadUInt16();

        if (total != data.Length - 10 || tlvLen > total)
            return false;

        int consumed = 0;
        while (consumed + 3 <= tlvLen)
        {
            var tag = (NspTag)reader.ReadByte();
            ushort len = reader.ReadUInt16();
            if (consumed + 3 + len > tlvLen)
                return false;

            package.Tlvs.Add(new NspTlv { Tag = tag, Value = reader.ReadBytes(len) });
            consumed += 3 + len;
        }

        package.TrailingData = reader.ReadBytes((int)total - tlvLen);
        return true;
    }

    public override string ToString() =>
        $"NSP cmd={Command}(0x{(byte)Command:X2}) req={RequestId} [{string.Join(", ", Tlvs)}]"
        + (TrailingData.Length > 0 ? $" +{TrailingData.Length}B" : "");
}
