using System.Text;

namespace ReLiveWP.Services.Push.Nsp;

public class NspTlv
{
    public NspTag Tag { get; set; }
    public byte[] Value { get; set; } = [];

    public int WireLength => 3 + Value.Length;

    public string AsString() => Encoding.Latin1.GetString(Value);

    public uint AsUInt() => Value.Length >= 4 ? BitConverter.ToUInt32(Value, 0)
                          : Value.Length > 0 ? Value[0] : 0u;

    public static NspTlv Byte(NspTag tag, byte value) => new() { Tag = tag, Value = [value] };
    public static NspTlv UInt(NspTag tag, uint value) => new() { Tag = tag, Value = BitConverter.GetBytes(value) };
    public static NspTlv String(NspTag tag, string value) => new() { Tag = tag, Value = Encoding.ASCII.GetBytes(value) };

    public override string ToString()
    {
        var rendered = Tag.IsString() ? $"\"{AsString()}\"" : Convert.ToHexString(Value);
        return $"{Tag}(0x{(byte)Tag:X2})={rendered}";
    }
}
