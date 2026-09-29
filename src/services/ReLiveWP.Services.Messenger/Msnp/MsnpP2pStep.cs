using System.Text;

namespace ReLiveWP.Services.Messenger.Msnp;

public readonly record struct MsnpP2pStep(string Step, string? BodyType)
{
    public const string DataStep = "data";

    private static readonly string[] Methods = ["INVITE", "ACK", "BYE", "CANCEL"];

    private static ReadOnlySpan<byte> SlpVersion => "MSNSLP/1.0"u8;
    private static ReadOnlySpan<byte> SlpContentType => "Content-Type: application/x-msnmsgr-"u8;
    private static ReadOnlySpan<byte> LineEnd => "\r\n"u8;

    public override string ToString() => BodyType is null ? Step : $"{Step} {BodyType}";

    public static MsnpP2pStep Describe(ReadOnlySpan<byte> payload)
    {
        var at = payload.IndexOf(SlpVersion);
        if (at < 0)
            return new MsnpP2pStep(DataStep, null);

        var step = ReadStatus(payload[(at + SlpVersion.Length)..]) ?? FindMethod(payload) ?? "other";
        return new MsnpP2pStep(step, ReadBodyType(payload[at..]));
    }

    private static string? ReadStatus(ReadOnlySpan<byte> afterVersion)
    {
        if (afterVersion.Length < 4 || afterVersion[0] != (byte)' ')
            return null;

        var code = afterVersion.Slice(1, 3);
        foreach (var digit in code)
        {
            if (digit is < (byte)'0' or > (byte)'9')
                return null;
        }

        return Encoding.ASCII.GetString(code);
    }

    private static string? FindMethod(ReadOnlySpan<byte> payload)
    {
        foreach (var method in Methods)
        {
            if (payload.IndexOf(Encoding.ASCII.GetBytes(method + " MSNMSGR:")) >= 0)
                return method;
        }

        return null;
    }

    private static string? ReadBodyType(ReadOnlySpan<byte> slp)
    {
        var at = slp.IndexOf(SlpContentType);
        if (at < 0)
            return null;

        var value = slp[(at + SlpContentType.Length)..];
        var end = value.IndexOf(LineEnd);
        return Encoding.ASCII.GetString(end < 0 ? value : value[..end]);
    }
}
