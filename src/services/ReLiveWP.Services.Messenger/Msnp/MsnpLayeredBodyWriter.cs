using System.Text;

namespace ReLiveWP.Services.Messenger.Msnp;

public sealed class MsnpLayeredBodyWriter
{
    private readonly StringBuilder headers = new();

    public MsnpLayeredBodyWriter AddHeader(string name, string value)
    {
        if (name.Length == 0 || name.AsSpan().IndexOfAny(":\r\n") >= 0)
            throw new ArgumentException($"'{name}' isn't a usable header name", nameof(name));

        if (value.AsSpan().IndexOfAny('\r', '\n') >= 0)
            throw new ArgumentException($"the {name} header can't span lines", nameof(value));

        headers.Append(name).Append(": ").Append(value).Append("\r\n");
        return this;
    }

    public MsnpLayeredBodyWriter EndBlock()
    {
        headers.Append("\r\n");
        return this;
    }

    public byte[] ToPayload(ReadOnlySpan<byte> content)
    {
        var headerBytes = Encoding.UTF8.GetBytes(headers.ToString());
        return [.. headerBytes, .. content];
    }
}
